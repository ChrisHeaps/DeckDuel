"use client";

import {
  Toaster as ChakraToaster,
  Toast,
  createToaster,
  Spinner,
  Stack,
} from "@chakra-ui/react";

export const toaster = createToaster({
  placement: "bottom-end",
  pauseOnPageIdle: true,
});

export const Toaster = () => {
  return (
    <ChakraToaster toaster={toaster}>
      {(toast) => (
        <Toast.Root
          key={toast.id}
          bg="white"
          p={4}
          borderRadius="md"
          boxShadow="lg"
          width={{ md: "sm" }}
        >
          <Stack gap={2} flex={1}>
            {toast.type === "loading" ? (
              <Spinner size="sm" color="blue.solid" />
            ) : (
              <Toast.Indicator color="teal.600" />
            )}
            <Stack gap={1}>
              {toast.title && (
                <Toast.Title color="gray.900">{toast.title}</Toast.Title>
              )}
              {toast.description && (
                <Toast.Description color="gray.700">
                  {toast.description}
                </Toast.Description>
              )}
            </Stack>
          </Stack>
          {toast.closable && <Toast.CloseTrigger color="gray.700" />}
        </Toast.Root>
      )}
    </ChakraToaster>
  );
};
