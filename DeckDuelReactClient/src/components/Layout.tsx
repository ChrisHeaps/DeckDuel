import { Flex, Box, Image } from "@chakra-ui/react";
import { Outlet } from "react-router-dom";
import Header from "./Header";
import { useAuth } from "../context/AuthContext";
import loggedOutDash from "../assets/loggedOutDash.png";

export default function Layout() {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return (
      <Flex direction="column" height="100vh">
        <Header />
        <Box
          flex="1"
          display="flex"
          alignItems="center"
          justifyContent="center"
          bg="gray.50"
          overflow="hidden"
        >
          <Image
            src={loggedOutDash}
            alt="Please log in"
            width="100%"
            height="100%"
            objectFit="cover"
            objectPosition="top center"
          />
        </Box>
        <Box
          as="footer"
          py={3}
          px={6}
          bg="gray.800"
          color="white"
          textAlign="center"
          fontSize="sm"
        >
          2026 Deck Duel. A Full Stack demo project - Chris Heaps.
        </Box>
      </Flex>
    );
  }

  return (
    <Flex direction="column" height="100vh">
      <Header />

      <Flex flex="1" overflow="hidden">
        <Outlet />
      </Flex>

      <Box
        as="footer"
        py={3}
        px={6}
        bg="gray.800"
        color="white"
        textAlign="center"
        fontSize="sm"
      >
        2026 Deck Duel. A Full Stack demo project - Chris Heaps.
      </Box>
    </Flex>
  );
}
