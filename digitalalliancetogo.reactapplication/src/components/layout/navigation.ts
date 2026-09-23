import {
    ClipboardList, CreditCard, FileText, History, LayoutDashboard, Package, Truck, Wallet, Wrench, type LucideIcon,
} from 'lucide-react';
import { Roles, type Role } from '@/lib/roles';

export interface EntreeNavigation {
    chemin: string;
    libelle: string;
    icone: LucideIcon;
    /** Rôles autorisés (l'Admin voit tout). */
    roles: Role[];
}

/** Menu du back-office : chaque rôle ne voit que ce qui le concerne. */
export const NAVIGATION: EntreeNavigation[] = [
    { chemin: '/', libelle: 'Tableau de bord', icone: LayoutDashboard, roles: [Roles.Commercial, Roles.GestionnaireStock, Roles.Technicien] },
    { chemin: '/devis', libelle: 'Devis', icone: FileText, roles: [Roles.Commercial] },
    { chemin: '/paiements', libelle: 'Paiements', icone: CreditCard, roles: [Roles.Commercial] },
    { chemin: '/commandes', libelle: 'Commandes', icone: ClipboardList, roles: [Roles.Commercial, Roles.GestionnaireStock] },
    { chemin: '/stock', libelle: 'Stock', icone: Package, roles: [Roles.GestionnaireStock, Roles.Commercial] },
    { chemin: '/livraisons', libelle: 'Livraisons', icone: Truck, roles: [Roles.GestionnaireStock, Roles.Livreur] },
    { chemin: '/sav', libelle: 'SAV', icone: Wrench, roles: [Roles.Commercial, Roles.Technicien, Roles.GestionnaireStock] },
    { chemin: '/finance', libelle: 'Remboursements & avoirs', icone: Wallet, roles: [Roles.Commercial] },
    { chemin: '/audit', libelle: 'Journal d\'audit', icone: History, roles: [] },
];
