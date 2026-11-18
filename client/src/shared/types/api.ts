/**
 * Phản chiếu chính xác 100% Contract PagedResult từ Backend .NET
 * (xem HuyetMach175.Modules.*.DTOs.Common.PagedResult.cs)
 */
export interface ApiPagedResult<T> {
    items: T[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages?: number
}

export type PagedResult<T> = ApiPagedResult<T>

export interface BasePageQuery {
    pageNumber?: number
    pageSize?: number
    keyword?: string
    search?: string
}
