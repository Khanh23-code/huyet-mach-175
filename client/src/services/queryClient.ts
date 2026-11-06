import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 60_000, // 1 phút không gọi lại API
            gcTime: 10 * 60_000,
            refetchOnWindowFocus: false, // quay lại tab không refetch
            retry: (count, err: any) =>
                ![401, 403, 404].includes(err?.response?.status) && count < 1,
        },
    },
})