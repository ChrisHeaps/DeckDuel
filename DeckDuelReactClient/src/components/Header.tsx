import {
  Flex,
  Heading,
  Spacer,
  Button,
  Dialog,
  Portal,
  Text,
} from "@chakra-ui/react";
import { useState } from "react";
import LoginForm from "./LoginForm";
import RegisterForm from "./RegisterForm";
import { useAuth } from "../context/AuthContext";

export default function Header() {
  const [isLoginOpen, setIsLoginOpen] = useState(false);
  const [isRegisterOpen, setIsRegisterOpen] = useState(false);
  const { isAuthenticated, inGameName, logout } = useAuth();

  return (
    <>
      <Flex
        as="header"
        align="center"
        px={6}
        py={4}
        bg="teal.600"
        color="white"
        boxShadow="md"
      >
        <Heading size="md">Deck Duel</Heading>
        <Spacer />

        {isAuthenticated ? (
          <Flex align="center" gap={3}>
            <Text fontSize="sm" fontWeight="medium">
              {inGameName}
            </Text>
            <Button size="sm" onClick={logout}>
              Logout
            </Button>
          </Flex>
        ) : (
          <Flex gap={2}>
            <Dialog.Root
              open={isLoginOpen}
              onOpenChange={(details) => setIsLoginOpen(details.open)}
            >
              <Dialog.Trigger asChild>
                <Button size="sm">Login</Button>
              </Dialog.Trigger>

              <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                  <Dialog.Content p={6} bg="white">
                    <Dialog.CloseTrigger />
                    <Dialog.Body>
                      <LoginForm />
                    </Dialog.Body>
                  </Dialog.Content>
                </Dialog.Positioner>
              </Portal>
            </Dialog.Root>

            <Dialog.Root
              open={isRegisterOpen}
              onOpenChange={(details) => setIsRegisterOpen(details.open)}
            >
              <Dialog.Trigger asChild>
                <Button size="sm">Register</Button>
              </Dialog.Trigger>

              <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                  <Dialog.Content p={6} bg="white">
                    <Dialog.CloseTrigger />
                    <Dialog.Body>
                      <RegisterForm />
                    </Dialog.Body>
                  </Dialog.Content>
                </Dialog.Positioner>
              </Portal>
            </Dialog.Root>
          </Flex>
        )}
      </Flex>
    </>
  );
}
