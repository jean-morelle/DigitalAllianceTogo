import { useLocation, useNavigate } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import type { Panier } from '@/lib/types';

/** Le panier n'existe que pour un client connecté. */
export function usePanier() {
    const { session } = useAuth();
    const estClient = !!session?.roles.includes(Roles.Client);
    return useQuery({
        queryKey: ['panier'],
        queryFn: () => api.get<Panier>('/panier'),
        enabled: estClient,
    });
}

/** Fixe une quantité ; un visiteur non connecté est envoyé à la connexion puis ramené ici. */
export function useQuantitePanier() {
    const { session } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const queryClient = useQueryClient();

    const mutation = useMutation({
        mutationFn: ({ produitId, quantite }: { produitId: string; quantite: number; message?: string }) =>
            api.put<Panier>(`/panier/lignes/${produitId}`, { quantite }),
        onSuccess: (panier, v) => {
            queryClient.setQueryData(['panier'], panier);
            if (v.message) toast.success(v.message);
        },
        onError: e => toast.error(e.message),
    });

    const definir = (produitId: string, quantite: number, message?: string) => {
        if (!session) {
            toast.info('Connectez-vous ou créez un compte pour ajouter au panier.');
            navigate('/connexion', { state: { depuis: location.pathname } });
            return;
        }
        if (!session.roles.includes(Roles.Client)) {
            toast.error('Le panier est réservé aux comptes clients.');
            return;
        }
        mutation.mutate({ produitId, quantite, message });
    };

    return {
        enCours: mutation.isPending,
        /** Fixe la quantité (écran panier). */
        definir,
        /** Ajoute au panier : s'additionne à la quantité déjà présente (boutique, fiche produit). */
        ajouter: (produitId: string, quantite: number, message?: string) => {
            const actuelle = queryClient.getQueryData<Panier>(['panier'])?.lignes.find(l => l.produitId === produitId)?.quantite ?? 0;
            definir(produitId, Math.min(actuelle + quantite, 100), message);
        },
    };
}
