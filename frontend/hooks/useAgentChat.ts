import { useMutation } from "@tanstack/react-query";
import { agentService } from "@/services/api";

export function useAgentChat() {
  const mutation = useMutation({
    mutationFn: (message: string) => agentService.chat(message),
  });

  return {
    sendMessage: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  };
}
