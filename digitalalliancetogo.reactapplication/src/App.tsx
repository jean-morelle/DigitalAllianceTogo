import { BrowserRouter, Navigate, Route, Routes } from 'react-router';
import { Construction } from 'lucide-react';
import { AppShell } from '@/components/layout/AppShell';
import { RedirectionAccueil, RequireAuth, RequireRole } from '@/components/layout/Garde';
import { EnTetePage } from '@/components/commun';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import { LoginPage } from '@/pages/LoginPage';
import { TableauDeBordPage } from '@/pages/TableauDeBordPage';
import { PaiementsPage } from '@/pages/PaiementsPage';
import { CommandesPage } from '@/pages/commandes/CommandesPage';
import { CommandeDetailPage } from '@/pages/commandes/CommandeDetailPage';

function BientotDisponible({ titre }: { titre: string }) {
    return (
        <>
            <EnTetePage titre={titre} />
            <div className="text-muted-foreground flex flex-col items-center gap-3 py-16">
                <Construction className="size-10" />
                Cet écran arrive dans la prochaine étape.
            </div>
        </>
    );
}

function Accueil() {
    const { aRole } = useAuth();
    return aRole(Roles.Commercial, Roles.GestionnaireStock, Roles.Technicien) ? <TableauDeBordPage /> : <RedirectionAccueil />;
}

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/connexion" element={<LoginPage />} />
                <Route element={<RequireAuth><AppShell /></RequireAuth>}>
                    <Route index element={<Accueil />} />
                    <Route path="paiements" element={<RequireRole roles={[Roles.Commercial]}><PaiementsPage /></RequireRole>} />
                    <Route path="commandes" element={<RequireRole roles={[Roles.Commercial, Roles.GestionnaireStock]}><CommandesPage /></RequireRole>} />
                    <Route path="commandes/:id" element={<RequireRole roles={[Roles.Commercial, Roles.GestionnaireStock]}><CommandeDetailPage /></RequireRole>} />
                    <Route path="devis" element={<BientotDisponible titre="Devis" />} />
                    <Route path="stock" element={<BientotDisponible titre="Stock" />} />
                    <Route path="livraisons" element={<BientotDisponible titre="Livraisons" />} />
                    <Route path="sav" element={<BientotDisponible titre="SAV" />} />
                    <Route path="finance" element={<BientotDisponible titre="Remboursements & avoirs" />} />
                    <Route path="audit" element={<BientotDisponible titre="Journal d'audit" />} />
                </Route>
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </BrowserRouter>
    );
}
