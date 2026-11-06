import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface ChatTurn {
  role: "user" | "agent";
  content: string;
}

interface AgentChatState {
  history: ChatTurn[];
  addTurn: (turn: ChatTurn) => void;
  clear: () => void;

  // Mirrors useAppStore's flag: persisted state rehydrates from localStorage
  // asynchronously after mount, so the thread is always empty on the very first render.
  hasHydrated: boolean;
  setHasHydrated: (value: boolean) => void;
}

// Deliberately its own store, not part of useAppStore — this thread belongs to the
// /agent screen only and has nothing to do with the auth session it tracks.
export const useAgentChatStore = create<AgentChatState>()(
  persist(
    (set) => ({
      history: [],
      addTurn: (turn) => set((state) => ({ history: [...state.history, turn] })),
      clear: () => set({ history: [] }),

      hasHydrated: false,
      setHasHydrated: (value) => set({ hasHydrated: value }),
    }),
    {
      name: "agent-chat-history",
      partialize: (state) => ({ history: state.history }),
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
    }
  )
);
