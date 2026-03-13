import {
  Box,
  Button,
  Flex,
  Heading,
  Spinner,
  Text,
  VStack,
  HStack,
} from "@chakra-ui/react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
import { useAuth } from "../context/AuthContext";
import { toaster } from "./ui/toaster";

type Category = {
  id: number;
  categoryTypeId?: number;
  CategoryTypeId?: number;
  description: string;
  score: number;
};

type Card = {
  id: number;
  name: string;
  categories: Category[];
};

type GameCardResponse = Card & {
  myTurn?: boolean;
  MyTurn?: boolean;
};

type TurnChangedPayload = {
  gameId: string;
  currentTurnUserId: string;
  turnNumber: number;
};

export default function GameView() {
  const { id } = useParams<{ id: string }>();
  const { token } = useAuth();

  const [connectionStatus, setConnectionStatus] = useState(
    "Connecting to game hub...",
  );
  const [turn, setTurn] = useState<TurnChangedPayload | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [card, setCard] = useState<Card | null>(null);
  const [loadingCard, setLoadingCard] = useState(true);
  const [cardError, setCardError] = useState<string | null>(null);
  const [isMyTurn, setIsMyTurn] = useState(false);
  const [submittingCategoryId, setSubmittingCategoryId] = useState<
    number | null
  >(null);

  const fetchCard = useCallback(async () => {
    if (!id) {
      setCardError("Missing game ID in route");
      setLoadingCard(false);
      return;
    }

    try {
      setCardError(null);
      setLoadingCard(true);
      const response = await apiFetch(
        `https://localhost:7119/games/usergames/${id}/card`,
      );
      const data = (await response.json()) as GameCardResponse;
      setCard(data);
      setIsMyTurn(data.myTurn ?? data.MyTurn ?? false);
    } catch (fetchError) {
      console.error("Failed to load game card:", fetchError);
      setCardError("Failed to load card");
    } finally {
      setLoadingCard(false);
    }
  }, [id]);

  useEffect(() => {
    fetchCard();
  }, [fetchCard]);

  const handleCategoryClick = async (categoryTypeId: number) => {
    if (!id || !isMyTurn) {
      return;
    }

    try {
      setSubmittingCategoryId(categoryTypeId);
      await apiFetch(`https://localhost:7119/games/${id}/turns`, {
        method: "POST",
        body: JSON.stringify({ CategoryTypeId: categoryTypeId }),
      });

      toaster.create({
        title: "Turn submitted",
        type: "success",
      });

      await fetchCard();
    } catch (submitError) {
      console.error("Failed to submit turn:", submitError);
      toaster.create({
        title: "Failed to submit turn",
        type: "error",
      });
    } finally {
      setSubmittingCategoryId(null);
    }
  };

  useEffect(() => {
    if (!id) {
      setError("Missing game ID in route");
      setConnectionStatus("Not connected");
      return;
    }

    if (!token) {
      setError("Missing auth token. Please login again.");
      setConnectionStatus("Not connected");
      return;
    }

    let connection: HubConnection | null = null;
    let isDisposed = false;
    const gameId = id;
    const parsedGameId = Number(gameId);

    if (Number.isNaN(parsedGameId)) {
      setError("Invalid game ID in route");
      setConnectionStatus("Not connected");
      return;
    }

    const getErrorMessage = (value: unknown) => {
      return value instanceof Error ? value.message : "Unknown SignalR error";
    };

    const isNegotiationStop = (message: string) => {
      return message.toLowerCase().includes("stopped during negotiation");
    };

    const connect = async () => {
      try {
        setError(null);
        setConnectionStatus("Connecting to game hub...");

        connection = new HubConnectionBuilder()
          .withUrl("https://localhost:7119/hubs/games", {
            accessTokenFactory: () => token,
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build();

        connection.on("TurnChanged", (payload: TurnChangedPayload) => {
          if (isDisposed) {
            return;
          }

          if (String(payload.gameId) === String(gameId)) {
            setTurn(payload);
          }
        });

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

        try {
          await connection.invoke("JoinGameGroup", parsedGameId);
        } catch (joinError) {
          setError(
            `Connected to hub, but failed to join game group: ${getErrorMessage(joinError)}`,
          );
        }
      } catch (connectionError) {
        const message = getErrorMessage(connectionError);

        if (isDisposed || isNegotiationStop(message)) {
          console.info(
            "SignalR connect attempt ended during cleanup:",
            message,
          );
          return;
        }

        setError(`Failed to connect to game notifications: ${message}`);
        setConnectionStatus("Connection failed");
        console.error("SignalR connection error:", connectionError);
      }
    };

    connect();

    return () => {
      isDisposed = true;

      if (!connection) {
        return;
      }

      if (connection.state === HubConnectionState.Connected) {
        connection
          .invoke("LeaveGameGroup", parsedGameId)
          .catch(() => undefined);
      }

      connection.stop().catch(() => undefined);
    };
  }, [id, token]);

  return (
    <Flex
      flex="1"
      direction="column"
      bg="gray.50"
      p={8}
      overflow="auto"
      gap={6}
    >
      <Box bg="white" borderRadius="xl" boxShadow="sm" p={6} w="full">
        <VStack align="stretch" gap={4}>
          <Heading size="lg">Game View</Heading>
          <Text color="gray.600">Game ID: {id}</Text>
          <Text>
            Hub Status: <Text as="span">{connectionStatus}</Text>
          </Text>
          <Text>
            Turn Status:{" "}
            <Text as="span">{isMyTurn ? "My Turn" : "Opponent Turn"}</Text>
          </Text>

          {error ? (
            <Text color="red.500">{error}</Text>
          ) : !turn ? (
            <Flex align="center" gap={3}>
              <Spinner size="sm" />
              <Text color="gray.600">Waiting for turn updates...</Text>
            </Flex>
          ) : (
            <Box bg="gray.50" p={4} borderRadius="md">
              <Text fontWeight="bold" mb={2}>
                Latest Turn Update
              </Text>
              <Text>Turn Number: {turn.turnNumber}</Text>
              <Text>Current Turn User ID: {turn.currentTurnUserId}</Text>
            </Box>
          )}
        </VStack>
      </Box>

      {loadingCard ? (
        <Flex align="center" justify="center" p={6}>
          <Spinner size="xl" />
        </Flex>
      ) : cardError ? (
        <Text color="red.500">{cardError}</Text>
      ) : !card ? (
        <Text color="gray.500">No card available for this game</Text>
      ) : (
        <Flex justify="center" align="center" h="full" p={4}>
          <Box
            bg="white"
            borderRadius="xl"
            boxShadow="lg"
            p={8}
            minW="400px"
            maxW="500px"
            w="full"
          >
            <VStack gap={6} align="stretch">
              <Heading size="xl" textAlign="center" color="teal.600">
                {card.name}
              </Heading>

              <Box>
                <Heading size="sm" mb={4} color="gray.600">
                  Stats
                </Heading>
                <VStack gap={3} align="stretch">
                  {card.categories.map((category) => {
                    const categoryTypeId =
                      category.categoryTypeId ??
                      category.CategoryTypeId ??
                      category.id;

                    return (
                      <Box
                        key={`${card.id}-${categoryTypeId}-${category.description}`}
                      >
                        <HStack justify="space-between" mb={1}>
                          <Button
                            variant="ghost"
                            size="xs"
                            p={0}
                            h="auto"
                            minH="unset"
                            fontWeight="medium"
                            color={isMyTurn ? "teal.700" : "gray.700"}
                            cursor={isMyTurn ? "pointer" : "default"}
                            textDecoration={isMyTurn ? "underline" : "none"}
                            disabled={
                              !isMyTurn || submittingCategoryId !== null
                            }
                            loading={submittingCategoryId === categoryTypeId}
                            onClick={() => handleCategoryClick(categoryTypeId)}
                          >
                            {category.description}
                          </Button>
                          <Text fontWeight="bold" color="teal.600">
                            {category.score}
                          </Text>
                        </HStack>
                        <Box
                          h="8px"
                          bg="gray.200"
                          borderRadius="full"
                          overflow="hidden"
                        >
                          <Box
                            h="full"
                            bg="teal.500"
                            w={`${category.score}%`}
                            transition="width 0.3s"
                          />
                        </Box>
                      </Box>
                    );
                  })}
                </VStack>
              </Box>
            </VStack>
          </Box>
        </Flex>
      )}
    </Flex>
  );
}
