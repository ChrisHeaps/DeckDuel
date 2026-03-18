import {
  Badge,
  Box,
  Button,
  Flex,
  Heading,
  Spinner,
  Text,
  VStack,
  HStack,
} from "@chakra-ui/react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { apiFetch } from "../api/apiClient";
import { buildApiUrl } from "../api/config";
import { useAuth } from "../context/AuthContext";
import { useGameHub } from "../hooks/useGameHub";
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
  gameId?: number;
  GameId?: number;
  myTurn?: boolean;
  MyTurn?: boolean;
};

type TurnChangedPayload = {
  gameId: string;
  currentTurnUserId: string;
  turnNumber: number;
};

type GameStatusPlayerDto = {
  userGameId?: number;
  UserGameId?: number;
  inGameName: string;
  handCardCount: number;
  currentTurnScore?: number | null;
};

type GameStatusDto = {
  gameId?: number;
  GameId?: number;
  winningUserInGameName?: string | null;
  WinningUserInGameName?: string | null;
  winningUserGameId?: number | null;
  WinningUserGameId?: number | null;
  currentRoundCategoryName?: string | null;
  players: GameStatusPlayerDto[];
};

export default function GameView() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { token } = useAuth();
  const [turn, setTurn] = useState<TurnChangedPayload | null>(null);
  const [card, setCard] = useState<Card | null>(null);
  const [loadingCard, setLoadingCard] = useState(true);
  const [cardError, setCardError] = useState<string | null>(null);
  const [isMyTurn, setIsMyTurn] = useState(false);
  const [submittingCategoryId, setSubmittingCategoryId] = useState<
    number | null
  >(null);
  const [gameStatus, setGameStatus] = useState<GameStatusDto | null>(null);
  const [resolvedGameId, setResolvedGameId] = useState<number | null>(null);
  const [gameFinished, setGameFinished] = useState(false);

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
        buildApiUrl(`/games/usergames/${id}/card`),
      );
      const data = (await response.json()) as GameCardResponse;
      setCard(data);
      setIsMyTurn(data.myTurn ?? data.MyTurn ?? false);

      const gameIdFromCard = data.gameId ?? data.GameId;
      if (typeof gameIdFromCard === "number") {
        setResolvedGameId(gameIdFromCard);
      }
    } catch (fetchError) {
      console.error("Failed to load game card:", fetchError);
      setCardError("Failed to load card");
    } finally {
      setLoadingCard(false);
    }
  }, [id]);

  const fetchGameStatus = useCallback(async () => {
    if (!id) return;
    try {
      const response = await apiFetch(
        buildApiUrl(`/games/usergames/${id}/status`),
      );
      const data = (await response.json()) as GameStatusDto;
      setGameStatus(data);

      const gameIdFromStatus = data.gameId ?? data.GameId;
      if (typeof gameIdFromStatus === "number") {
        setResolvedGameId(gameIdFromStatus);
      }
    } catch (statusError) {
      console.error("Failed to load game status:", statusError);
    }
  }, [id]);

  const fetchCardRef = useRef(fetchCard);
  const fetchGameStatusRef = useRef(fetchGameStatus);

  useEffect(() => {
    fetchCardRef.current = fetchCard;
  }, [fetchCard]);

  useEffect(() => {
    fetchGameStatusRef.current = fetchGameStatus;
  }, [fetchGameStatus]);

  useEffect(() => {
    fetchCard();
  }, [fetchCard]);

  useEffect(() => {
    fetchGameStatus();
  }, [fetchGameStatus]);

  const hubHandlers = useMemo(
    () => ({
      TurnChanged: (payload: unknown) => {
        const turnPayload = payload as TurnChangedPayload;
        if (String(turnPayload.gameId) === String(resolvedGameId)) {
          setTurn(turnPayload);
          fetchCardRef.current();
          fetchGameStatusRef.current();
        }
      },
      GameFinished: () => {
        setGameFinished(true);
        setIsMyTurn(false);
        fetchGameStatusRef.current();
      },
    }),
    [resolvedGameId],
  );

  const { connectionStatus, connectionError } = useGameHub({
    token,
    handlers: hubHandlers,
    gameGroupId: resolvedGameId ?? undefined,
    enabled: !!id,
  });

  const handleCategoryClick = async (categoryTypeId: number) => {
    if (!id || !isMyTurn) {
      return;
    }

    try {
      setSubmittingCategoryId(categoryTypeId);
      await apiFetch(buildApiUrl(`/games/usergames/${id}/turns`), {
        method: "POST",
        body: JSON.stringify({ CategoryTypeId: categoryTypeId }),
      });

      toaster.create({
        title: "Turn submitted",
        type: "success",
      });

      await Promise.all([fetchCard(), fetchGameStatus()]);
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

  return (
    <Flex
      flex="1"
      direction="column"
      bg="gray.50"
      p={8}
      overflow="auto"
      gap={6}
    >
      <Box>
        <Button onClick={() => navigate("/")} size="sm">
          ← Home
        </Button>
      </Box>

      <Box bg="white" borderRadius="xl" boxShadow="sm" p={6} w="full">
        <VStack align="stretch" gap={4}>
          <Heading size="lg">Game View</Heading>
          <Text color="gray.600">UserGame ID: {id}</Text>
          <Text>
            Hub Status: <Text as="span">{connectionStatus}</Text>
          </Text>
          <Text>
            Turn Status:{" "}
            <Text as="span">{isMyTurn ? "My Turn" : "Opponent Turn"}</Text>
          </Text>

          {connectionError ? (
            <Text color="red.500">{connectionError}</Text>
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

      {gameStatus && gameStatus.players.length > 0 && (
        <Flex gap={3} flexWrap="wrap" justify="center">
          {gameStatus.players.map((player) => {
            const winningName =
              gameStatus.winningUserInGameName ??
              gameStatus.WinningUserInGameName;
            const winningUserGameId =
              gameStatus.winningUserGameId ?? gameStatus.WinningUserGameId;
            const playerUserGameId = player.userGameId ?? player.UserGameId;

            const isWinner =
              (winningUserGameId != null &&
                playerUserGameId != null &&
                winningUserGameId === playerUserGameId) ||
              (!!winningName &&
                player.inGameName.toLowerCase() === winningName.toLowerCase());

            return (
              <Box
                key={player.inGameName}
                bg={isWinner ? "yellow.50" : "white"}
                borderRadius="lg"
                boxShadow="sm"
                p={4}
                minW="160px"
                borderWidth={isWinner ? "2px" : "1px"}
                borderColor={isWinner ? "yellow.300" : "gray.200"}
              >
                <VStack align="stretch" gap={2}>
                  <HStack justify="space-between" align="start">
                    <Text fontWeight="bold" fontSize="sm">
                      {player.inGameName}
                    </Text>
                    {isWinner ? (
                      <Badge colorPalette="yellow">Winner!</Badge>
                    ) : null}
                  </HStack>
                  <HStack justify="space-between">
                    <Text fontSize="xs" color="gray.500">
                      Cards in hand
                    </Text>
                    <Badge>{player.handCardCount}</Badge>
                  </HStack>
                  {player.currentTurnScore != null && (
                    <HStack justify="space-between">
                      <Text fontSize="xs" color="gray.500">
                        Turn score
                      </Text>
                      <Badge colorPalette="teal">
                        {player.currentTurnScore}
                      </Badge>
                    </HStack>
                  )}
                </VStack>
              </Box>
            );
          })}
        </Flex>
      )}

      {gameFinished ? null : loadingCard ? (
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

                    const isActiveCategory =
                      !!gameStatus?.currentRoundCategoryName &&
                      category.description.toLowerCase() ===
                        gameStatus.currentRoundCategoryName.toLowerCase();

                    const isRoundCategoryLocked =
                      isMyTurn && !!gameStatus?.currentRoundCategoryName;

                    const canSelectCategory =
                      isMyTurn && (!isRoundCategoryLocked || isActiveCategory);

                    return (
                      <Box
                        key={`${card.id}-${categoryTypeId}-${category.description}`}
                        bg={isActiveCategory ? "teal.50" : undefined}
                        borderRadius={isActiveCategory ? "md" : undefined}
                        px={isActiveCategory ? 2 : 0}
                        borderLeftWidth={isActiveCategory ? "3px" : undefined}
                        borderLeftColor={
                          isActiveCategory ? "teal.400" : undefined
                        }
                      >
                        <HStack justify="space-between" mb={1}>
                          <Button
                            variant="ghost"
                            size="xs"
                            p={0}
                            h="auto"
                            minH="unset"
                            fontWeight="medium"
                            color={canSelectCategory ? "teal.700" : "gray.700"}
                            cursor={canSelectCategory ? "pointer" : "default"}
                            textDecoration={
                              canSelectCategory ? "underline" : "none"
                            }
                            disabled={
                              !canSelectCategory ||
                              submittingCategoryId !== null
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
