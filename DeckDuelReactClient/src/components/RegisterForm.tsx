import { Box, Button, Input, VStack, Heading, Field } from "@chakra-ui/react";
import { useDialogContext } from "@chakra-ui/react";
import { useState } from "react";
import { register } from "../api/auth";
import { toaster } from "./ui/toaster";

type RegisterFormData = {
  username: string;
  password: string;
  ingamename: string;
  email: string;
};

export default function RegisterForm() {
  const dialog = useDialogContext();
  const [formData, setFormData] = useState<RegisterFormData>({
    username: "",
    password: "",
    ingamename: "",
    email: "",
  });

  const [errors, setErrors] = useState<Partial<RegisterFormData>>({});
  const [loading, setLoading] = useState(false);

  const validate = () => {
    const newErrors: Partial<RegisterFormData> = {};

    if (!formData.username) {
      newErrors.username = "Username is required";
    } else if (formData.username.length > 50) {
      newErrors.username = "Username must be 50 characters or less";
    }

    if (!formData.password) newErrors.password = "Password is required";

    if (!formData.ingamename) {
      newErrors.ingamename = "In-game name is required";
    } else if (formData.ingamename.length > 30) {
      newErrors.ingamename = "In-game name must be 30 characters or less";
    }

    if (!formData.email) {
      //newErrors.email = "Email is required";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = "Invalid email format";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    const payload = {
      username: formData.username,
      password: formData.password,
      email: formData.email,
      ingamename: formData.ingamename,
    };

    try {
      setLoading(true);
      await register(payload);

      toaster.create({
        title: "Registration successful",
        description: "You can now log in with your credentials",
        type: "success",
      });

      // Close dialog using context
      dialog.setOpen(false);
    } catch (error) {
      toaster.create({
        title: "Registration failed",
        description: error instanceof Error ? error.message : "Unknown error",
        type: "error",
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Heading size="md" mb={4}>
        Register
      </Heading>

      <form onSubmit={handleSubmit}>
        <VStack gap={4}>
          <Field.Root invalid={!!errors.username}>
            <Field.Label>Username</Field.Label>
            <Input
              type="text"
              value={formData.username}
              onChange={(e) =>
                setFormData({ ...formData, username: e.target.value })
              }
            />
            <Field.ErrorText>{errors.username}</Field.ErrorText>
          </Field.Root>

          <Field.Root invalid={!!errors.password}>
            <Field.Label>Password</Field.Label>
            <Input
              type="password"
              value={formData.password}
              onChange={(e) =>
                setFormData({ ...formData, password: e.target.value })
              }
            />
            <Field.ErrorText>{errors.password}</Field.ErrorText>
          </Field.Root>

          <Field.Root invalid={!!errors.ingamename}>
            <Field.Label>In-game Name</Field.Label>
            <Input
              type="text"
              value={formData.ingamename}
              onChange={(e) =>
                setFormData({ ...formData, ingamename: e.target.value })
              }
            />
            <Field.ErrorText>{errors.ingamename}</Field.ErrorText>
          </Field.Root>

          <Field.Root invalid={!!errors.email}>
            <Field.Label>Email *optional</Field.Label>
            <Input
              type="email"
              value={formData.email}
              onChange={(e) =>
                setFormData({ ...formData, email: e.target.value })
              }
            />
            <Field.ErrorText>{errors.email}</Field.ErrorText>
          </Field.Root>

          <Button
            type="submit"
            colorPalette="teal"
            loading={loading}
            width="full"
          >
            Register
          </Button>
        </VStack>
      </form>
    </Box>
  );
}
