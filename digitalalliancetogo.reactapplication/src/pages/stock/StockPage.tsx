import { useSearchParams } from 'react-router';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { EnTetePage } from '@/components/commun';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import { EtatStock } from './EtatStock';
import { Ecarts, Entrepots, Reception } from './OngletsGestionStock';

/** Stock (§11, §28-30). Le Commercial consulte ; le Gestionnaire de stock et l'Admin gèrent. */
export function StockPage() {
    const { aRole } = useAuth();
    const gestion = aRole(Roles.GestionnaireStock);
    const [params, setParams] = useSearchParams();
    const onglet = params.get('onglet') ?? 'etat';

    const changerOnglet = (valeur: string) => {
        const suivants = new URLSearchParams(valeur === 'etat' ? params : undefined);
        if (valeur === 'etat') suivants.delete('onglet'); else suivants.set('onglet', valeur);
        setParams(suivants);
    };

    return (
        <>
            <EnTetePage titre="Stock" description="Disponible, réservé, en transit et défectueux, par entrepôt." />
            <Tabs value={gestion ? onglet : 'etat'} onValueChange={changerOnglet}>
                {gestion && (
                    <TabsList className="mb-4">
                        <TabsTrigger value="etat">État du stock</TabsTrigger>
                        <TabsTrigger value="reception">Réception</TabsTrigger>
                        <TabsTrigger value="ecarts">Surplus fournisseur</TabsTrigger>
                        <TabsTrigger value="entrepots">Entrepôts</TabsTrigger>
                    </TabsList>
                )}
                <TabsContent value="etat"><EtatStock gestion={gestion} /></TabsContent>
                {gestion && (
                    <>
                        <TabsContent value="reception"><Reception /></TabsContent>
                        <TabsContent value="ecarts"><Ecarts /></TabsContent>
                        <TabsContent value="entrepots"><Entrepots gestion={gestion} /></TabsContent>
                    </>
                )}
            </Tabs>
        </>
    );
}
