import {
  Box,
  Button,
  Flex,
  Heading,
  Spinner,
  Stack,
  Text,
} from "@chakra-ui/react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
import { buildApiUrl } from "../api/config";
import { toaster } from "./ui/toaster";

type DeckInfo = {
  id: number;
  topic: string;
  isOwned?: boolean;
};

export default function CreateGame() {
  const navigate = useNavigate();
  const [decks, setDecks] = useState<DeckInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [selectedDeckId, setSelectedDeckId] = useState<number | null>(null);

  useEffect(() => {
    const fetchDecks = async () => {
      try {
        const response = await apiFetch(buildApiUrl("/decks"));
        const data = await response.json();
        setDecks(data);
      } catch (error) {
        console.error("Failed to load decks:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchDecks();
  }, []);

  const selectedDeck = useMemo(
    () => decks.find((deck) => deck.id === selectedDeckId),
    [decks, selectedDeckId],
  );

  const handleCreateGame = async () => {
    if (!selectedDeckId) {
      toaster.create({
        title: "Choose a deck first",
        type: "warning",
      });
      return;
    }

    try {
      setCreating(true);

      await apiFetch(buildApiUrl("/games"), {
        method: "POST",
        body: JSON.stringify({ deckId: selectedDeckId }),
      });

      toaster.create({
        title: "Game created",
        description: `Using deck: ${selectedDeck?.topic ?? selectedDeckId}`,
        type: "success",
      });

      navigate("/");
    } catch (error) {
      console.error("Failed to create game:", error);
      toaster.create({
        title: "Failed to create game",
        type: "error",
      });
    } finally {
      setCreating(false);
    }
  };

  return (
    <Flex
      flex="1"
      justify="center"
      align="start"
      p={6}
      bg="gray.50"
      overflow="auto"
    >
      <Box
        w="full"
        maxW="900px"
        bg="white"
        borderRadius="xl"
        boxShadow="sm"
        p={6}
      >
        <Heading size="lg" mb={2}>
          Create New Game
        </Heading>
        <Text color="gray.600" mb={6}>
          Choose a deck to create a new game.
        </Text>

        {loading ? (
          <Spinner />
        ) : decks.length > 0 ? (
          <Flex gap={3} flexWrap="wrap">
            {decks.map((deck) => {
              const isSelected = deck.id === selectedDeckId;
              return (
                <Button
                  key={deck.id}
                  variant={isSelected ? "solid" : "outline"}
                  colorPalette={isSelected ? "teal" : "gray"}
                  onClick={() => setSelectedDeckId(deck.id)}
                >
                  {deck.topic}
                </Button>
              );
            })}
          </Flex>
        ) : (
          <Text color="gray.500">No decks available to choose from.</Text>
        )}

        <Stack direction="row" gap={3} mt={8}>
          <Button
            colorPalette="teal"
            onClick={handleCreateGame}
            disabled={decks.length === 0 || creating}
            loading={creating}
          >
            Create Game
          </Button>
          <Button
            variant="outline"
            onClick={() => navigate("/")}
            disabled={creating}
          >
            Cancel
          </Button>
        </Stack>
      </Box>
    </Flex>
  );
}
