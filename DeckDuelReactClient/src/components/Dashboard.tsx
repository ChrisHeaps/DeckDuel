import {
  Box,
  Heading,
  Stack,
  Text,
  Spinner,
  Flex,
  Button,
  Badge,
} from "@chakra-ui/react";
import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
import { buildApiUrl } from "../api/config";
import { useAuth } from "../context/AuthContext";
import { useGameHub } from "../hooks/useGameHub";
import DashboardItemCard from "./DashboardItemCard";

type DeckInfo = {
  id: number;
  topic: string;
  isOwned?: boolean;
};

type GameInfo = {
  userGameId: number;
  deckTopic: string;
  usernames: string[];
  myTurn?: boolean;
  MyTurn?: boolean;
};

type OpenGameInfo = {
  gameId: number;
  deckTopic: string;
  isOwned: boolean;
  userNames: string[];
  joined?: boolean;
  Joined?: boolean;
};

export default function Dashboard() {
  const navigate = useNavigate();
  const { token } = useAuth();
  const [myGames, setMyGames] = useState<GameInfo[]>([]);
  const [openGames, setOpenGames] = useState<OpenGameInfo[]>([]);
  const [myDecks, setMyDecks] = useState<DeckInfo[]>([]);
  const [sharedDecks, setSharedDecks] = useState<DeckInfo[]>([]);
  const [loadingMyGames, setLoadingMyGames] = useState(true);
  const [loadingOpenGames, setLoadingOpenGames] = useState(true);
  const [loadingMyDecks, setLoadingMyDecks] = useState(true);
  const [actingGameId, setActingGameId] = useState<number | null>(null);

  const fetchMyGames = useCallback(async () => {
    try {
      setLoadingMyGames(true);
      const response = await apiFetch(buildApiUrl("/games/active"));
      const data = await response.json();
      console.log("Fetched my games:", data);
      setMyGames(data);
    } catch (error) {
      console.error("Failed to load my games:", error);
    } finally {
      setLoadingMyGames(false);
    }
  }, []);

  const fetchMyDecks = useCallback(async () => {
    try {
      setLoadingMyDecks(true);
      const response = await apiFetch(buildApiUrl("/decks"));
      const data = await response.json();
      setMyDecks(data);
    } catch (error) {
      console.error("Failed to load my decks:", error);
    } finally {
      setLoadingMyDecks(false);
    }
  }, []);

  const fetchOpenGames = useCallback(async () => {
    try {
      setLoadingOpenGames(true);
      const response = await apiFetch(buildApiUrl("/games/open"));
      const data = await response.json();
      console.log("Fetched open games:", data);
      setOpenGames(data);
    } catch (error) {
      console.error("Failed to load open games:", error);
    } finally {
      setLoadingOpenGames(false);
    }
  }, []);

  const fetchSharedDecks = useCallback(async () => {
    try {
      setLoadingSharedDecks(true);
      const response = await apiFetch(buildApiUrl("/getSharedDeckNames"));
      const data = await response.json();
      setSharedDecks(data);
    } catch (error) {
      console.error("Failed to load shared decks:", error);
    } finally {
      setLoadingSharedDecks(false);
    }
  }, []);

  const fetchMyGamesRef = useRef(fetchMyGames);
  const fetchOpenGamesRef = useRef(fetchOpenGames);

  useEffect(() => {
    fetchMyGamesRef.current = fetchMyGames;
  }, [fetchMyGames]);

  useEffect(() => {
    fetchOpenGamesRef.current = fetchOpenGames;
  }, [fetchOpenGames]);

  const hubHandlers = useMemo(
    () => ({
      GameOpened: () => {
        fetchMyGamesRef.current();
        fetchOpenGamesRef.current();
      },
      GameJoined: () => {
        fetchMyGamesRef.current();
        fetchOpenGamesRef.current();
      },
      GameStarted: () => {
        fetchMyGamesRef.current();
        fetchOpenGamesRef.current();
      },
      GameFinished: () => {
        fetchMyGamesRef.current();
        fetchOpenGamesRef.current();
      },
    }),
    [],
  );

  useGameHub({ token, handlers: hubHandlers, enabled: !!token });

  useEffect(() => {
    fetchMyGames();
    fetchMyDecks();
    fetchOpenGames();
    fetchSharedDecks();
  }, [fetchMyGames, fetchMyDecks, fetchOpenGames, fetchSharedDecks]);

  const handleOpenGameAction = async (game: OpenGameInfo) => {
    const endpoint = game.isOwned
      ? buildApiUrl(`/games/${game.gameId}/start`)
      : buildApiUrl(`/games/${game.gameId}/players`);

    try {
      setActingGameId(game.gameId);
      await apiFetch(endpoint, { method: "POST" });
      await Promise.all([fetchMyGames(), fetchOpenGames()]);
    } catch (error) {
      console.error(
        `Failed to ${game.isOwned ? "start" : "join"} game ${game.gameId}:`,
        error,
      );
    } finally {
      setActingGameId(null);
    }
  };

  return (
    <Stack gap={8} p={4} flex="1" minH={0} overflowY="auto">
      <Section title="My Active Games">
        <Stack gap={3}>
          <Flex gap={4} flexWrap="wrap">
            <DashboardItemCard
              label="Create New Game"
              onClick={() => navigate("/game/create")}
            />
            {!loadingMyGames &&
              myGames.map((game) => {
                const isMyTurn = game.myTurn ?? game.MyTurn ?? false;
                return (
                  <DashboardItemCard
                    key={game.userGameId}
                    label={game.deckTopic}
                    badgeLabel={isMyTurn ? "My Turn" : undefined}
                    badgeColorPalette="purple"
                    onClick={() => navigate(`/usergame/${game.userGameId}`)}
                  />
                );
              })}
          </Flex>
          {loadingMyGames ? <Spinner /> : null}
          {!loadingMyGames && myGames.length === 0 ? (
            <Text color="gray.500">No games available - create a new game</Text>
          ) : null}
        </Stack>
      </Section>

      <Section title="Open Games">
        {loadingOpenGames ? (
          <Spinner />
        ) : openGames.length > 0 ? (
          <Flex gap={4} flexWrap="wrap">
            {openGames.map((game) => {
              const hasJoined = game.joined ?? game.Joined ?? false;
              const showActionButton = game.isOwned || !hasJoined;
              const cannotStartYet = game.isOwned && game.userNames.length <= 1;

              return (
                <Box
                  key={game.gameId}
                  width="240px"
                  p={4}
                  bg={game.isOwned ? "teal.50" : "gray.100"}
                  borderWidth="1px"
                  borderColor={game.isOwned ? "teal.300" : "gray.200"}
                  borderRadius="md"
                  display="flex"
                  flexDirection="column"
                  gap={3}
                  cursor="default"
                  onClick={(event) => event.stopPropagation()}
                >
                  <Flex justify="space-between" align="start" gap={2}>
                    <Text fontWeight="semibold">{game.deckTopic}</Text>
                    <Badge colorPalette={game.isOwned ? "green" : "gray"}>
                      {game.isOwned ? "Owned" : "Waiting"}
                    </Badge>
                  </Flex>

                  <Box>
                    <Text fontSize="sm" color="gray.600" mb={1}>
                      Players
                    </Text>
                    <Text fontSize="sm" color="gray.800">
                      {game.userNames.length > 0
                        ? game.userNames.join(", ")
                        : "No players yet"}
                    </Text>
                  </Box>

                  {showActionButton ? (
                    <Button
                      size="sm"
                      colorPalette={game.isOwned ? "teal" : "blue"}
                      onClick={(event) => {
                        event.stopPropagation();
                        handleOpenGameAction(game);
                      }}
                      loading={actingGameId === game.gameId}
                      disabled={actingGameId !== null || cannotStartYet}
                    >
                      {game.isOwned ? "Start" : "Join"}
                    </Button>
                  ) : null}
                </Box>
              );
            })}
          </Flex>
        ) : (
          <Text color="gray.500">No games available to join</Text>
        )}
      </Section>

      <Section title="Decks">
        <Stack gap={3}>
          <Flex gap={4} flexWrap="wrap">
            <DashboardItemCard
              label="Generate New Deck"
              onClick={() => navigate("/deck/generate")}
            />
            {!loadingMyDecks &&
              myDecks.map((deck) => (
                <DashboardItemCard
                  key={deck.id}
                  label={deck.topic}
                  isOwned={deck.isOwned}
                  onClick={() => navigate(`/deck/${deck.id}`)}
                />
              ))}
          </Flex>
          {loadingMyDecks ? <Spinner /> : null}
          {!loadingMyDecks && myDecks.length === 0 ? (
            <Text color="gray.500">No decks available</Text>
          ) : null}
        </Stack>
      </Section>
    </Stack>
  );
}

type SectionProps = {
  title: string;
  children?: React.ReactNode;
};

function Section({ title, children }: SectionProps) {
  return (
    <Box
      bg="white"
      p={6}
      borderRadius="xl"
      boxShadow="sm"
      borderWidth="1px"
      borderColor="gray.200"
    >
      <Heading size="md" mb={4}>
        {title}
      </Heading>

      {children}
    </Box>
  );
}
