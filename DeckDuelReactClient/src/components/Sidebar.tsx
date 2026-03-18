import { Box, VStack, Button, Heading } from "@chakra-ui/react";
import { apiFetch } from "../api/apiClient";
import { buildApiUrl } from "../api/config";

export default function Sidebar() {
  return (
    <Box
      width="250px"
      bg="gray.100"
      p={4}
      borderRight="1px solid"
      borderColor="gray.200"
    >
      <Heading size="sm" mb={4}>
        Your Decks
      </Heading>

      <VStack align="stretch">
        <Button
          variant="ghost"
          onClick={async () => {
            try {
              const response = await apiFetch(buildApiUrl("/getMyDecks"));
              const decks = await response.json();
              console.log("Decks:", decks);
              // Update state with decks here
            } catch (error) {
              console.error("Failed to load decks:", error);
            }
          }}
        >
          Animals
        </Button>
        <Button variant="ghost">Cars</Button>
        <Button variant="ghost">Dinosaurs</Button>
        <Button colorScheme="teal">Generate New Deck</Button>
      </VStack>
    </Box>
  );
}
