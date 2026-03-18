import {
  Box,
  Button,
  Field,
  Flex,
  Heading,
  Input,
  Stack,
  Text,
} from "@chakra-ui/react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
import { buildApiUrl } from "../api/config";
import { toaster } from "./ui/toaster";

export default function GenerateDeck() {
  const navigate = useNavigate();
  const [deckPrompt, setDeckPrompt] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const validate = () => {
    if (!deckPrompt.trim()) {
      setError("Deck prompt is required");
      return false;
    }

    if (deckPrompt.trim().length > 30) {
      setError("Deck prompt must be 30 characters or less");
      return false;
    }

    setError(null);
    return true;
  };

  const handleGenerate = async () => {
    if (!validate()) {
      return;
    }

    try {
      setLoading(true);
      await apiFetch(buildApiUrl("/generateDeck"), {
        method: "POST",
        body: JSON.stringify({ deckPrompt: deckPrompt.trim() }),
      });

      toaster.create({
        title: "Deck generation started",
        type: "success",
      });

      navigate("/");
    } catch (apiError) {
      console.error("Failed to generate deck:", apiError);
      toaster.create({
        title: "Failed to generate deck",
        type: "error",
      });
    } finally {
      setLoading(false);
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
          Generate Deck
        </Heading>
        <Text color="gray.600" mb={6}>
          Here's the fun part. Describe the deck you want to generate. This will
          be fed to a top secret LLM located on the far side of the moon. It
          will then work its magic and create a 30 card deck based on your
          description. Once the deck is generated, it will appear in your
          dashboard and be ready to play with!
        </Text>

        <Field.Root invalid={!!error} mb={6}>
          <Input
            type="text"
            value={deckPrompt}
            maxLength={30}
            onChange={(event) => setDeckPrompt(event.target.value)}
            placeholder="e.g. Pubs in central Manchester"
          />
          <Field.HelperText>{deckPrompt.length}/30</Field.HelperText>
          <Field.ErrorText>{error}</Field.ErrorText>
        </Field.Root>

        <Stack direction="row" gap={3}>
          <Button
            colorPalette="teal"
            onClick={handleGenerate}
            loading={loading}
          >
            Generate Deck
          </Button>
          <Button
            variant="outline"
            onClick={() => navigate("/")}
            disabled={loading}
          >
            Cancel
          </Button>
        </Stack>
      </Box>
    </Flex>
  );
}
