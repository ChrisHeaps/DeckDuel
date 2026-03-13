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
import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
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
  const [myGames, setMyGames] = useState<GameInfo[]>([]);
  const [openGames, setOpenGames] = useState<OpenGameInfo[]>([]);
  const [myDecks, setMyDecks] = useState<DeckInfo[]>([]);
  const [sharedDecks, setSharedDecks] = useState<DeckInfo[]>([]);
  const [loadingMyGames, setLoadingMyGames] = useState(true);
  const [loadingOpenGames, setLoadingOpenGames] = useState(true);
  const [loadingMyDecks, setLoadingMyDecks] = useState(true);
  const [loadingSharedDecks, setLoadingSharedDecks] = useState(true);
  const [actingGameId, setActingGameId] = useState<number | null>(null);

  useEffect(() => {
    const fetchMyGames = async () => {
      try {
        const response = await apiFetch("https://localhost:7119/games/active");
        const data = await response.json();
        console.log("Fetched my games:", data);
        setMyGames(data);
      } catch (error) {
        console.error("Failed to load my games:", error);
      } finally {
        setLoadingMyGames(false);
      }
    };

    const fetchMyDecks = async () => {
      try {
        const response = await apiFetch("https://localhost:7119/decks");
        const data = await response.json();
        setMyDecks(data);
      } catch (error) {
        console.error("Failed to load my decks:", error);
      } finally {
        setLoadingMyDecks(false);
      }
    };

    const fetchOpenGames = async () => {
      try {
        const response = await apiFetch("https://localhost:7119/games/open");
        const data = await response.json();
        console.log("Fetched open games:", data);
        setOpenGames(data);
      } catch (error) {
        console.error("Failed to load open games:", error);
      } finally {
        setLoadingOpenGames(false);
      }
    };

    const fetchSharedDecks = async () => {
      try {
        const response = await apiFetch(
          "https://localhost:7119/getSharedDeckNames",
        );
        const data = await response.json();
        setSharedDecks(data);
      } catch (error) {
        console.error("Failed to load shared decks:", error);
      } finally {
        setLoadingSharedDecks(false);
      }
    };

    fetchMyGames();
    fetchMyDecks();
    fetchOpenGames();
    fetchSharedDecks();
  }, []);

  const handleOpenGameAction = async (game: OpenGameInfo) => {
    const endpoint = game.isOwned
      ? `https://localhost:7119/games/${game.gameId}/start`
      : `https://localhost:7119/games/${game.gameId}/players`;

    try {
      setActingGameId(game.gameId);
      await apiFetch(endpoint, { method: "POST" });
      navigate(`/game/${game.gameId}`);
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
                    onClick={() => navigate(`/game/${game.userGameId}`)}
                  />
                );
              })}
          </Flex>
          {loadingMyGames ? <Spinner /> : null}
          {!loadingMyGames && myGames.length === 0 ? (
            <Text color="gray.500">No games available</Text>
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

              return (
                <Box
                  key={game.gameId}
                  width="240px"
                  p={4}
                  bg={game.isOwned ? "teal.50" : "gray.50"}
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
                      {game.isOwned ? "Owned" : "Open"}
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
                      disabled={actingGameId !== null}
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

      <Section title="Shared Decks">
        {loadingSharedDecks ? (
          <Spinner />
        ) : sharedDecks.length > 0 ? (
          <Flex gap={4} flexWrap="wrap">
            {sharedDecks.map((deck) => (
              <DashboardItemCard
                key={deck.id}
                label={deck.topic}
                onClick={() => navigate(`/deck/${deck.id}`)}
              />
            ))}
          </Flex>
        ) : (
          <Text color="gray.500">No shared decks available</Text>
        )}
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
