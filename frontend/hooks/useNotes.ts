import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { noteService, type NoteListParams } from "@/services/api";
import type { CreateNoteRequest, UpdateNoteRequest } from "@/types";

export const noteKeys = {
  all: ["notes"] as const,
  lists: () => [...noteKeys.all, "list"] as const,
  list: (params: NoteListParams = {}) => [...noteKeys.lists(), params] as const,
  latest: (count: number) => [...noteKeys.all, "latest", count] as const,
  details: () => [...noteKeys.all, "detail"] as const,
  detail: (id: string) => [...noteKeys.details(), id] as const,
};

export function useNotes(params: NoteListParams = {}) {
  return useQuery({
    queryKey: noteKeys.list(params),
    queryFn: () => noteService.list(params),
  });
}

export function useLatestNotes(count = 10) {
  return useQuery({
    queryKey: noteKeys.latest(count),
    queryFn: () => noteService.latest(count),
  });
}

export function useNote(id: string) {
  return useQuery({
    queryKey: noteKeys.detail(id),
    queryFn: () => noteService.getById(id),
    enabled: id.length > 0,
  });
}

export function useCreateNote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateNoteRequest) => noteService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.all });
    },
  });
}

export function useUpdateNote(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateNoteRequest) => noteService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: noteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: noteKeys.all });
    },
  });
}

export function useDeleteNote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => noteService.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.all });
    },
  });
}
