import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";
import { API_BASE_URL } from "../api/config";

type SignalREventHandlers = Record<string, (payload: unknown) => void>;

type UseGameHubOptions = {
  token: string | null;
  handlers: SignalREventHandlers;
  gameGroupId?: number;
  enabled?: boolean;
};

type UseGameHubResult = {
  connectionStatus: string;
  connectionError: string | null;
};

export function useGameHub({
  token,
  handlers,
  gameGroupId,
  enabled = true,
}: UseGameHubOptions): UseGameHubResult {
  const [connectionStatus, setConnectionStatus] = useState("Not connected");
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const handlersRef = useRef(handlers);

  useEffect(() => {
    handlersRef.current = handlers;
  }, [handlers]);

  useEffect(() => {
    if (!enabled) {
      setConnectionStatus("Not connected");
      setConnectionError(null);
      return;
    }

    if (!token) {
      setConnectionStatus("Not connected");
      setConnectionError("Missing auth token. Please login again.");
      return;
    }

    if (gameGroupId !== undefined && Number.isNaN(gameGroupId)) {
      setConnectionStatus("Not connected");
      setConnectionError("Invalid game ID in route");
      return;
    }

    let connection: HubConnection | null = null;
    let isDisposed = false;

    const getErrorMessage = (value: unknown) => {
      return value instanceof Error ? value.message : "Unknown SignalR error";
    };

    const isNegotiationStop = (message: string) => {
      return message.toLowerCase().includes("stopped during negotiation");
    };

    const connect = async () => {
      try {
        setConnectionError(null);
        setConnectionStatus("Connecting to game hub...");

        connection = new HubConnectionBuilder()
          .withUrl(`${API_BASE_URL}/hubs/games`, {
            accessTokenFactory: () => token,
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build();

        for (const eventName of Object.keys(handlersRef.current)) {
          connection.on(eventName, (payload: unknown) => {
            if (isDisposed) {
              return;
            }

            const handler = handlersRef.current[eventName];
            if (handler) {
              handler(payload);
            }
          });
        }

        connection.onreconnecting(() => {
          if (isDisposed) {
            return;
          }

          setConnectionStatus("Reconnecting...");
        });

        connection.onreconnected(() => {
          if (isDisposed) {
            return;
          }

          setConnectionStatus("Connected");
        });

        connection.onclose(() => {
          if (isDisposed) {
            return;
          }

          setConnectionStatus("Disconnected");
        });

        await connection.start();
        setConnectionStatus("Connected");

        if (gameGroupId !== undefined) {
          try {
            await connection.invoke("JoinGameGroup", gameGroupId);
          } catch (joinError) {
            setConnectionError(
              `Connected to hub, but failed to join game group: ${getErrorMessage(joinError)}`,
            );
          }
        }
      } catch (connectionErrorValue) {
        const message = getErrorMessage(connectionErrorValue);

        if (isDisposed || isNegotiationStop(message)) {
          console.info(
            "SignalR connect attempt ended during cleanup:",
            message,
          );
          return;
        }

        setConnectionError(`Failed to connect to game notifications: ${message}`);
        setConnectionStatus("Connection failed");
        console.error("SignalR connection error:", connectionErrorValue);
      }
    };

    connect();

    return () => {
      isDisposed = true;

      if (!connection) {
        return;
      }

      if (
        gameGroupId !== undefined &&
        connection.state === HubConnectionState.Connected
      ) {
        connection.invoke("LeaveGameGroup", gameGroupId).catch(() => undefined);
      }

      connection.stop().catch(() => undefined);
    };
  }, [enabled, token, gameGroupId]);

  return {
    connectionStatus,
    connectionError,
  };
}
