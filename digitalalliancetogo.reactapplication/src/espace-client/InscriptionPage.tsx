import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router';
import { Loader2 } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';

/** Canaux d'arrivée (§37) : mesure de ce que rapportent les réseaux sociaux. */
const SOURCES: [string, string][] = [
    ['TikTok', 'TikTok'], ['WhatsApp', 'WhatsApp'], ['Facebook', 'Facebook'], ['Instagram', 'Instagram'],
    ['Recommandation', 'Un proche m\'en a parlé'], ['Boutique', 'En boutique'], ['SiteWeb', 'Recherche sur internet'], ['Autre', 'Autre'],
];

export function InscriptionPage() {
    const { session, connecter } = useAuth();
    const navigate = useNavigate();
    const [f, setF] = useState({ type: 'Particulier', nom: '', prenom: '', raisonSociale: '', telephone: '+228 ', email: '', motDePasse: '', source: '' });
    const [erreur, setErreur] = useState<string | null>(null);
    const [enCours, setEnCours] = useState(false);

    if (session) return <Navigate to="/compte" replace />;

    const champ = (cle: keyof typeof f) => ({ value: f[cle], onChange: (e: ChangeEvent<HTMLInputElement>) => setF(s => ({ ...s, [cle]: e.target.value })) });

    const soumettre = async (e: FormEvent) => {
        e.preventDefault();
        setErreur(null);
        setEnCours(true);
        try {
            await api.post('/clients/inscription', {
                type: f.type,
                nom: f.nom.trim(),
                prenom: f.prenom.trim() || null,
                raisonSociale: f.type === 'Entreprise' ? f.raisonSociale.trim() : null,
                telephone: f.telephone.replace(/\s/g, ''),
                email: f.email.trim(),
                motDePasse: f.motDePasse,
                source: f.source || 'SiteWeb',
            });
            await connecter(f.email.trim(), f.motDePasse);
            navigate('/boutique', { replace: true });
        } catch (err) {
            setErreur(err instanceof Error ? err.message : 'Inscription impossible.');
        } finally {
            setEnCours(false);
        }
    };

    return (
        <div className="mx-auto max-w-md">
            <Card>
                <CardHeader>
                    <CardTitle className="text-xl">Créer mon compte</CardTitle>
                    <CardDescription>Pour commander, suivre vos livraisons et votre SAV.</CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={soumettre} className="grid gap-4">
                        {erreur && <Alert variant="destructive"><AlertDescription>{erreur}</AlertDescription></Alert>}
                        <Select value={f.type} onValueChange={v => setF(s => ({ ...s, type: v }))}>
                            <SelectTrigger aria-label="Type de compte"><SelectValue /></SelectTrigger>
                            <SelectContent>
                                <SelectItem value="Particulier">Particulier</SelectItem>
                                <SelectItem value="Entreprise">Entreprise / professionnel</SelectItem>
                            </SelectContent>
                        </Select>
                        {f.type === 'Entreprise' && (
                            <div className="grid gap-2"><Label htmlFor="rs">Raison sociale *</Label><Input id="rs" required {...champ('raisonSociale')} /></div>
                        )}
                        <div className="grid grid-cols-2 gap-3">
                            <div className="grid gap-2"><Label htmlFor="prenom">Prénom</Label><Input id="prenom" autoComplete="given-name" {...champ('prenom')} /></div>
                            <div className="grid gap-2"><Label htmlFor="nom">Nom *</Label><Input id="nom" autoComplete="family-name" required {...champ('nom')} /></div>
                        </div>
                        <div className="grid gap-2"><Label htmlFor="tel">Téléphone (WhatsApp) *</Label><Input id="tel" type="tel" autoComplete="tel" required {...champ('telephone')} /></div>
                        <div className="grid gap-2"><Label htmlFor="email">E-mail *</Label><Input id="email" type="email" autoComplete="email" required {...champ('email')} /></div>
                        <div className="grid gap-2">
                            <Label htmlFor="mdp">Mot de passe *</Label>
                            <Input id="mdp" type="password" autoComplete="new-password" required minLength={8} {...champ('motDePasse')} />
                            <p className="text-muted-foreground text-xs">8 caractères minimum, dont une majuscule et un chiffre.</p>
                        </div>
                        <div className="grid gap-2">
                            <Label>Comment nous avez-vous connus ?</Label>
                            <Select value={f.source} onValueChange={v => setF(s => ({ ...s, source: v }))}>
                                <SelectTrigger><SelectValue placeholder="Choisir" /></SelectTrigger>
                                <SelectContent>{SOURCES.map(([v, l]) => <SelectItem key={v} value={v}>{l}</SelectItem>)}</SelectContent>
                            </Select>
                        </div>
                        <Button type="submit" disabled={enCours}>{enCours && <Loader2 className="animate-spin" />} Créer mon compte</Button>
                        <p className="text-muted-foreground text-center text-sm">Déjà client ? <Link to="/connexion" className="text-primary hover:underline">Se connecter</Link></p>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
