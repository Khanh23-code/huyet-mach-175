import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query'

import { permissionsApi } from './permissionsApi'

export const useRolePermissions = (roleId: number | undefined) =>
    useQuery({
        queryKey: ['role-permissions', roleId],
        queryFn: () => permissionsApi.getByRole(roleId!),
        enabled: roleId !== undefined,
    })

export const useUpdateRolePermissions = () => {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({
                         roleId,
                         permissionIds,
                     }: {
            roleId: number
            permissionIds: number[]
        }) => permissionsApi.updateByRole(roleId, { permissionIds }),

        onSuccess: async (data, variables) => {
            // BE trả về RolePermissionsDto mới nhất -> ghi thẳng vào cache, khỏi gọi lại GET.
            queryClient.setQueryData(['role-permissions', variables.roleId], data)

            await queryClient.invalidateQueries({ queryKey: ['roles'] })
        },
    })
}