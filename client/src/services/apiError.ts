import type { AxiosError } from 'axios'
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'

type RawError = {
    message?: string
    title?: string
    errors?: Record<string, string | string[]>
}

/** Đọc lỗi từ BE (cả format { message, errors } lẫn ProblemDetails). */
export const parseApiError = (err: unknown) => {
    const data = (err as AxiosError<RawError>)?.response?.data
    const fieldErrors: Record<string, string> = {}

    if (data?.errors) {
        for (const [key, val] of Object.entries(data.errors)) {
            const field = key.charAt(0).toLowerCase() + key.slice(1) // "Email" -> "email"
            fieldErrors[field] = Array.isArray(val) ? val[0] : val
        }
    }

    return {
        message: data?.message ?? data?.title ?? 'Có lỗi xảy ra, vui lòng thử lại',
        fieldErrors,
    }
}

/** Gắn lỗi từ server vào đúng ô input. Trả về true nếu có ít nhất 1 lỗi được gắn. */
export const applyServerErrors = <T extends FieldValues>(
    fieldErrors: Record<string, string>,
    setError: UseFormSetError<T>,
    fields: string[],
): boolean => {
    let applied = false
    for (const f of fields) {
        if (fieldErrors[f]) {
            setError(f as Path<T>, { type: 'server', message: fieldErrors[f] })
            applied = true
        }
    }
    return applied
}