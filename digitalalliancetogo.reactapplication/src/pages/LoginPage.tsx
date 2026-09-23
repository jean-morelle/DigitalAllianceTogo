import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router';
import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';

export function LoginPage() {
    const { session, connecter } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const [email, setEmail] = useState('');
    const [motDePasse, setMotDePasse] = useState('');
    const [erreur, setErreur] = useState<string | null>(null);
    const [enCours, setEnCours] = useState(false);

    if (session) return <Navigate to={session.roles.includes(Roles.Client) ? '/compte' : '/'} replace />;

    const soumettre = async (e: FormEvent) => {
        e.preventDefault();
        setErreur(null);
        setEnCours(true);
        try {
            const nouvelle = await connecter(email.trim(), motDePasse);
            const client = nouvelle.roles.includes(Roles.Client);
            const retour = (location.state as { depuis?: string } | null)?.depuis ?? (client ? '/compte' : '/');
            navigate(retour, { replace: true });
        } catch (err) {
            setErreur(err instanceof Error ? err.message : 'Connexion impossible.');
        } finally {
            setEnCours(false);
        }
    };

    return (
        <div className="bg-muted/30 flex min-h-svh items-center justify-center p-4">
            <Card className="w-full max-w-sm">
                <CardHeader className="text-center">
                    <div className="bg-primary text-primary-foreground mx-auto mb-2 flex size-12 items-center justify-center rounded-lg text-lg font-bold">TI</div>
                    <CardTitle className="text-xl">Togo Informatique</CardTitle>
                    <CardDescription>Clients et personnel</CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={soumettre} className="grid gap-4">
                        {erreur && (
                            <Alert variant="destructive">
                                <AlertDescription>{erreur}</AlertDescription>
                            </Alert>
                        )}
                        <div className="grid gap-2">
                            <Label htmlFor="email">E-mail</Label>
                            <Input id="email" type="email" autoComplete="username" required value={email} onChange={e => setEmail(e.target.value)} />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="motDePasse">Mot de passe</Label>
                            <Input id="motDePasse" type="password" autoComplete="current-password" required value={motDePasse} onChange={e => setMotDePasse(e.target.value)} />
                        </div>
                        <Button type="submit" disabled={enCours} className="w-full">
                            {enCours && <Loader2 className="animate-spin" />}
                            Se connecter
                        </Button>
                    </form>
                    <p className="text-muted-foreground mt-4 text-center text-sm">
                        Nouveau client ? <Link to="/inscription" className="text-primary hover:underline">Créer un compte</Link>
                        {' · '}<Link to="/boutique" className="text-primary hover:underline">Voir la boutique</Link>
                    </p>
                </CardContent>
            </Card>
        </div>
    );
}
