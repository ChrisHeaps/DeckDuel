import { Box, Button, Input, VStack, Heading, Field } from "@chakra-ui/react";
import { useDialogContext } from "@chakra-ui/react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
//import { login } from "../api/auth";
import { useAuth } from "../context/AuthContext";
import { toaster } from "./ui/toaster";

type LoginFormData = {
  username: string;
  password: string;
};

export default function LoginForm() {
  const { login } = useAuth();
  const dialog = useDialogContext();
  const navigate = useNavigate();
  const [formData, setFormData] = useState<LoginFormData>({
    username: "",
    password: "",
  });

  const [errors, setErrors] = useState<Partial<LoginFormData>>({});
  const [loading, setLoading] = useState(false);

  const validate = () => {
    const newErrors: Partial<LoginFormData> = {};

    if (!formData.username) newErrors.username = "Username is required";
    if (!formData.password) newErrors.password = "Password is required";

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.SubmitEvent) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    const payload = {
      username: formData.username,
      password: formData.password,
      email: null,
    };

    try {
      setLoading(true);

      await login(payload);

      toaster.create({
        title: "Login successful",
        type: "success",
      });

      navigate("/", { replace: true });

      // Close dialog using context
      dialog.setOpen(false);
    } catch (error) {
      toaster.create({
        title: "Login failed",
        type: "error",
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Heading size="md" mb={4}>
        Login
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

          <Button
            type="submit"
            colorPalette="teal"
            loading={loading}
            width="full"
          >
            Log In
          </Button>
        </VStack>
      </form>
    </Box>
  );
}
