import {
  Box,
  Flex,
  Heading,
  Text,
  Button,
  Spinner,
  VStack,
  HStack,
  Carousel,
  IconButton,
} from "@chakra-ui/react";
import { useState, useEffect, useMemo } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
import { LuChevronLeft, LuChevronRight } from "react-icons/lu";

type Category = {
  id: number;
  description: string;
  score: number;
};

type Card = {
  id: number;
  name: string;
  categories: Category[];
};

export default function DeckView() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [cards, setCards] = useState<Card[]>([]);
  const [loading, setLoading] = useState(true);

  const categoryOrder = useMemo(
    () => cards[0]?.categories.map((category) => category.description) ?? [],
    [cards],
  );

  useEffect(() => {
    const fetchCards = async () => {
      if (!id) return;

      try {
        const response = await apiFetch(
          `https://localhost:7119/decks/${id}/cards`,
        );
        const data = await response.json();
        setCards(data);
      } catch (error) {
        console.error("Failed to load deck cards:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchCards();
  }, [id]);

  if (loading) {
    return (
      <Flex flex="1" align="center" justify="center" bg="gray.50" p={6}>
        <Spinner size="xl" />
      </Flex>
    );
  }

  if (cards.length === 0) {
    return (
      <Flex
        flex="1"
        direction="column"
        align="center"
        justify="center"
        bg="gray.50"
        p={6}
        gap={4}
      >
        <Text fontSize="lg" color="gray.500">
          No cards found in this deck
        </Text>
        <Button onClick={() => navigate("/")}>Back to Dashboard</Button>
      </Flex>
    );
  }

  return (
    <Flex flex="1" direction="column" bg="gray.50" p={8} overflow="auto">
      <Box mb={6}>
        <Button onClick={() => navigate("/")} size="sm" mb={4}>
          ← Back to Dashboard
        </Button>
        <Heading size="lg">Deck Cards</Heading>
        <Text color="gray.600" mt={2}>
          {cards.length} {cards.length === 1 ? "card" : "cards"} in this deck
        </Text>
      </Box>

      {/* Chakra UI Carousel */}
      <Carousel.Root flex="1" slideCount={cards.length} loop>
        <Carousel.ItemGroup w="full">
          {cards.map((card, index) => (
            <Carousel.Item key={card.id} index={index} w="full">
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
                        {categoryOrder.map((description) => {
                          const category = card.categories.find(
                            (item) => item.description === description,
                          );

                          if (!category) {
                            return null;
                          }

                          return (
                            <Box key={`${card.id}-${description}`}>
                              <HStack justify="space-between" mb={1}>
                                <Text fontWeight="medium">{description}</Text>
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
            </Carousel.Item>
          ))}
        </Carousel.ItemGroup>

        <Carousel.Control mt={6} display="flex" justifyContent="center">
          <Carousel.PrevTrigger asChild>
            <IconButton variant="outline" size="lg">
              <LuChevronLeft />
            </IconButton>
          </Carousel.PrevTrigger>
          <Carousel.IndicatorGroup>
            {cards.map((_, index) => (
              <Carousel.Indicator key={index} index={index} />
            ))}
          </Carousel.IndicatorGroup>
          <Carousel.NextTrigger asChild>
            <IconButton variant="outline" size="lg">
              <LuChevronRight />
            </IconButton>
          </Carousel.NextTrigger>
        </Carousel.Control>
      </Carousel.Root>
    </Flex>
  );
}
