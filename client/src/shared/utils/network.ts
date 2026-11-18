import type { AxiosError } from 'axios'

/** Không có response = mất mạng / server không phản hồi. */
export const isNetworkError = (e: unknown): boolean => !(e as AxiosError)?.response
