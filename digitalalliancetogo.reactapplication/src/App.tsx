import { lazy, Suspense } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router';
import { Chargement } from '@/components/commun';
import { AppShell } from '@/components/layout/AppShell';
import { RedirectionAccueil, RequireAuth, RequireClient, RequireRole } from '@/components/layout/Garde';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import { LoginPage } from '@/pages/LoginPage';
import { MiseEnPageClient } from '@/espace-client/MiseEnPageClient';

// Chaque écran est chargé à la demande : la boutique publique ne télécharge pas le back-office
const TableauDeBordPage = lazy(() => import('@/pages/TableauDeBordPage').then(m => ({ default: m.TableauDeBordPage })));
const PaiementsPage = lazy(() => import('@/pages/PaiementsPage').then(m => ({ default: m.PaiementsPage })));
const CommandesPage = lazy(() => import('@/pages/commandes/CommandesPage').then(m => ({ default: m.CommandesPage })));
const CommandeDetailPage = lazy(() => import('@/pages/commandes/CommandeDetailPage').then(m => ({ default: m.CommandeDetailPage })));
const ProposerModificationPage = lazy(() => import('@/pages/commandes/ProposerModificationPage').then(m => ({ default: m.ProposerModificationPage })));
const DevisPage = lazy(() => import('@/pages/devis/DevisPage').then(m => ({ default: m.DevisPage })));
const DevisFormPage = lazy(() => import('@/pages/devis/DevisFormPage').then(m => ({ default: m.DevisFormPage })));
const DevisDetailPage = lazy(() => import('@/pages/devis/DevisDetailPage').then(m => ({ default: m.DevisDetailPage })));
const StockPage = lazy(() => import('@/pages/stock/StockPage').then(m => ({ default: m.StockPage })));
const LivraisonsPage = lazy(() => import('@/pages/livraisons/LivraisonsPage').then(m => ({ default: m.LivraisonsPage })));
const SavPage = lazy(() => import('@/pages/sav/SavPage').then(m => ({ default: m.SavPage })));
const SavDetailPage = lazy(() => import('@/pages/sav/SavDetailPage').then(m => ({ default: m.SavDetailPage })));
const FinancePage = lazy(() => import('@/pages/FinancePage').then(m => ({ default: m.FinancePage })));
const ParametresPage = lazy(() => import('@/pages/ParametresPage').then(m => ({ default: m.ParametresPage })));
const AuditPage = lazy(() => import('@/pages/AuditPage').then(m => ({ default: m.AuditPage })));
const BoutiquePage = lazy(() => import('@/espace-client/BoutiquePage').then(m => ({ default: m.BoutiquePage })));
const ProduitPage = lazy(() => import('@/espace-client/ProduitPage').then(m => ({ default: m.ProduitPage })));
const PanierPage = lazy(() => import('@/espace-client/PanierPage').then(m => ({ default: m.PanierPage })));
const InscriptionPage = lazy(() => import('@/espace-client/InscriptionPage').then(m => ({ default: m.InscriptionPage })));
const MesAdresses = lazy(() => import('@/espace-client/Adresses').then(m => ({ default: m.MesAdresses })));
const CompteLayout = lazy(() => import('@/espace-client/compte/CompteLayout').then(m => ({ default: m.CompteLayout })));
const MesAvoirs = lazy(() => import('@/espace-client/compte/Listes').then(m => ({ default: m.MesAvoirs })));
const MesCommandes = lazy(() => import('@/espace-client/compte/Listes').then(m => ({ default: m.MesCommandes })));
const MesDevis = lazy(() => import('@/espace-client/compte/Listes').then(m => ({ default: m.MesDevis })));
const MesTicketsSav = lazy(() => import('@/espace-client/compte/Listes').then(m => ({ default: m.MesTicketsSav })));
const CommandeClientPage = lazy(() => import('@/espace-client/compte/CommandeClientPage').then(m => ({ default: m.CommandeClientPage })));
const DevisClientPage = lazy(() => import('@/espace-client/compte/DevisClientPage').then(m => ({ default: m.DevisClientPage })));

function Accueil() {
    const { aRole } = useAuth();
    return aRole(Roles.Commercial, Roles.GestionnaireStock, Roles.Technicien) ? <TableauDeBordPage /> : <RedirectionAccueil />;
}

export default function App() {
    return (
        <BrowserRouter>
            <Suspense fallback={<div className="p-6"><Chargement lignes={6} /></div>}>
            <Routes>
                <Route path="/connexion" element={<LoginPage />} />

                {/* Espace client : boutique publique, panier et compte */}
                <Route element={<MiseEnPageClient />}>
                    <Route path="boutique" element={<BoutiquePage />} />
                    <Route path="boutique/produits/:id" element={<ProduitPage />} />
                    <Route path="inscription" element={<InscriptionPage />} />
                    <Route path="boutique/panier" element={<RequireClient><PanierPage /></RequireClient>} />
                    <Route path="compte" element={<RequireClient><CompteLayout /></RequireClient>}>
                        <Route index element={<MesCommandes />} />
                        <Route path="devis" element={<MesDevis />} />
                        <Route path="sav" element={<MesTicketsSav />} />
                        <Route path="avoirs" element={<MesAvoirs />} />
                        <Route path="adresses" element={<MesAdresses />} />
                    </Route>
                    <Route path="compte/commandes/:id" element={<RequireClient><CommandeClientPage /></RequireClient>} />
                    <Route path="compte/devis/:id" element={<RequireClient><DevisClientPage /></RequireClient>} />
                </Route>

                {/* Back-office du personnel */}
                <Route element={<RequireAuth><AppShell /></RequireAuth>}>
                    <Route index element={<Accueil />} />
                    <Route path="paiements" element={<RequireRole roles={[Roles.Commercial]}><PaiementsPage /></RequireRole>} />
                    <Route path="commandes" element={<RequireRole roles={[Roles.Commercial, Roles.GestionnaireStock]}><CommandesPage /></RequireRole>} />
                    <Route path="commandes/:id" element={<RequireRole roles={[Roles.Commercial, Roles.GestionnaireStock]}><CommandeDetailPage /></RequireRole>} />
                    <Route path="commandes/:id/modifier" element={<RequireRole roles={[Roles.Commercial]}><ProposerModificationPage /></RequireRole>} />
                    <Route path="devis" element={<RequireRole roles={[Roles.Commercial]}><DevisPage /></RequireRole>} />
                    <Route path="devis/nouveau" element={<RequireRole roles={[Roles.Commercial]}><DevisFormPage /></RequireRole>} />
                    <Route path="devis/:id" element={<RequireRole roles={[Roles.Commercial]}><DevisDetailPage /></RequireRole>} />
                    <Route path="devis/:id/modifier" element={<RequireRole roles={[Roles.Commercial]}><DevisFormPage /></RequireRole>} />
                    <Route path="stock" element={<RequireRole roles={[Roles.GestionnaireStock, Roles.Commercial]}><StockPage /></RequireRole>} />
                    <Route path="livraisons" element={<RequireRole roles={[Roles.GestionnaireStock, Roles.Livreur]}><LivraisonsPage /></RequireRole>} />
                    <Route path="sav" element={<RequireRole roles={[Roles.Commercial, Roles.Technicien, Roles.GestionnaireStock]}><SavPage /></RequireRole>} />
                    <Route path="sav/:id" element={<RequireRole roles={[Roles.Commercial, Roles.Technicien, Roles.GestionnaireStock]}><SavDetailPage /></RequireRole>} />
                    <Route path="finance" element={<RequireRole roles={[Roles.Commercial]}><FinancePage /></RequireRole>} />
                    <Route path="audit" element={<RequireRole roles={[]}><AuditPage /></RequireRole>} />
                    <Route path="parametres" element={<RequireRole roles={[]}><ParametresPage /></RequireRole>} />
                </Route>
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
            </Suspense>
        </BrowserRouter>
    );
}
