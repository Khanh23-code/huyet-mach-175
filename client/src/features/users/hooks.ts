import { useEffect, useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { referenceApi, usersApi } from './usersApi'
import type { UserQuery } from '@/types/user'

export const useDebounce = <T,>(value: T, delay = 400) => {
    const [v, setV] = useState(value)
    useEffect(() => {
        const t = setTimeout(() => setV(value), delay)
        return () => clearTimeout(t)
    }, [value, delay])
    return v
}

export const useUsers = (q: UserQuery) =>
    useQuery({
        queryKey: ['users', 'list', q],
        queryFn: () => usersApi.list(q),
        placeholderData: keepPreviousData,
    })

/** Chỉ lấy tổng số bản ghi (pageSize = 1) cho thẻ thống kê / đếm theo vai trò */
export const useUserCount = (filters: Pick<UserQuery, 'isActive' | 'roleCode' | 'roleCodes'> = {}) =>
    useQuery({
        queryKey: ['users', 'count', filters],
        queryFn: () => usersApi.list({ ...filters, pageNumber: 1, pageSize: 1 }),
        select: (d) => d.total,
    })

export const useRoles = () =>
    useQuery({ queryKey: ['roles'], queryFn: referenceApi.roles, staleTime: 5 * 60 * 1000 })

export const useDepartments = () =>
    useQuery({ queryKey: ['departments'], queryFn: referenceApi.departments, staleTime: 5 * 60 * 1000 })
