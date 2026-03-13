import { RouterProvider, createBrowserRouter } from "react-router-dom";
import Layout from "./components/Layout";
import Dashboard from "./components/Dashboard";
import DeckView from "./components/DeckView";
import GameView from "./components/GameView";
import CreateGame from "./components/CreateGame";
import GenerateDeck from "./components/GenerateDeck";
import { Toaster } from "./components/ui/toaster";
import { AuthProvider } from "./context/AuthContext";
import "./App.css";

const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      { path: "/", element: <Dashboard /> },
      { path: "/deck/:id", element: <DeckView /> },
      { path: "/deck/generate", element: <GenerateDeck /> },
      { path: "/game/:id", element: <GameView /> },
      { path: "/game/create", element: <CreateGame /> },
    ],
  },
]);

function App() {
  return (
    <AuthProvider>
      <Toaster />
      <RouterProvider router={router} />
    </AuthProvider>
  );
}

export default App;
