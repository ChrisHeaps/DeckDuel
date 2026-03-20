import { Badge, Box, Text } from "@chakra-ui/react";

type DashboardItemCardProps = {
  label: string;
  onClick?: () => void;
  isOwned?: boolean;
  badgeLabel?: string;
  badgeColorPalette?: string;
};

export default function DashboardItemCard({
  label,
  onClick,
  isOwned,
  badgeLabel,
  badgeColorPalette,
}: DashboardItemCardProps) {
  return (
    <Box
      onClick={onClick}
      width="130px"
      height="130px"
      p={2}
      bg={isOwned ? "teal.50" : "gray.100"}
      borderWidth="2px"
      borderColor={isOwned ? "teal.400" : "transparent"}
      borderRadius="md"
      _hover={{
        bg: isOwned ? "teal.100" : "gray.200",
        transform: "scale(1.02)",
      }}
      cursor={onClick ? "pointer" : "default"}
      display="flex"
      alignItems="center"
      justifyContent="center"
      position="relative"
      transition="all 0.2s"
    >
      {badgeLabel ? (
        <Badge
          position="absolute"
          top={2}
          right={2}
          colorPalette={badgeColorPalette ?? "blue"}
        >
          {badgeLabel}
        </Badge>
      ) : isOwned ? (
        <Badge position="absolute" top={2} right={2} colorPalette="green">
          Owned
        </Badge>
      ) : null}

      <Text
        fontSize="sm"
        fontWeight={isOwned ? "bold" : "medium"}
        textAlign="center"
      >
        {label}
      </Text>
    </Box>
  );
}
