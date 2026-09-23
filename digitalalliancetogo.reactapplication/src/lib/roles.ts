/** Rôles tels qu'émis par l'API (Application/Common/Security/Roles.cs). */
export const Roles = {
    Admin: 'Admin',
    Commercial: 'Commercial',
    GestionnaireStock: 'GestionnaireStock',
    Technicien: 'Technicien',
    Livreur: 'Livreur',
    Client: 'Client',
    Catalogue: 'Catalogue',
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];
