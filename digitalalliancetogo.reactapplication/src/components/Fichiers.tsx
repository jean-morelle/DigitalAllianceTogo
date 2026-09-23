import { useEffect, useRef, useState, type PointerEvent } from 'react';
import { Camera, Check, ExternalLink, FileText, Loader2, RotateCcw, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { api } from '@/lib/api';

const PREFIXE_INTERNE = '/api/fichiers/';

interface EnvoiProps {
    categorie: 'paiements' | 'livraisons';
    valeur: string | null;
    surChangement: (url: string | null) => void;
    libelle: string;
    /** Sur téléphone : ouvre directement l'appareil photo arrière. */
    camera?: boolean;
}

/** Choix (ou prise) d'une photo / d'un PDF, envoyé aussitôt à l'API ; aperçu local. */
export function EnvoiFichier({ categorie, valeur, surChangement, libelle, camera }: EnvoiProps) {
    const entree = useRef<HTMLInputElement>(null);
    const [apercu, setApercu] = useState<{ url: string; image: boolean; nom: string } | null>(null);
    const [envoi, setEnvoi] = useState(false);

    useEffect(() => () => { if (apercu) URL.revokeObjectURL(apercu.url); }, [apercu]);

    const choisir = async (fichier: File | undefined) => {
        if (!fichier) return;
        if (fichier.size > 5 * 1024 * 1024) { toast.error('Le fichier dépasse 5 Mo : réduisez la photo.'); return; }
        setEnvoi(true);
        try {
            const resultat = await api.envoyerFichier(categorie, fichier, fichier.name);
            setApercu({ url: URL.createObjectURL(fichier), image: fichier.type.startsWith('image/'), nom: fichier.name });
            surChangement(resultat.url);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Envoi impossible.');
        } finally {
            setEnvoi(false);
            if (entree.current) entree.current.value = '';
        }
    };

    const retirer = () => { setApercu(null); surChangement(null); };

    return (
        <div className="grid gap-2">
            <input ref={entree} type="file" className="hidden" accept="image/jpeg,image/png,image/webp,application/pdf"
                capture={camera ? 'environment' : undefined} onChange={e => void choisir(e.target.files?.[0])} />
            {valeur && apercu ? (
                <div className="flex items-center gap-3 rounded-md border p-2">
                    {apercu.image
                        ? <img src={apercu.url} alt="Aperçu" className="size-14 rounded object-cover" />
                        : <FileText className="text-muted-foreground size-10" />}
                    <div className="min-w-0 flex-1 text-sm">
                        <div className="flex items-center gap-1 text-emerald-700"><Check className="size-4" /> Envoyé</div>
                        <div className="text-muted-foreground truncate text-xs">{apercu.nom}</div>
                    </div>
                    <Button type="button" variant="ghost" size="icon" aria-label="Retirer" onClick={retirer}><X /></Button>
                </div>
            ) : (
                <Button type="button" variant="outline" disabled={envoi} onClick={() => entree.current?.click()}>
                    {envoi ? <Loader2 className="animate-spin" /> : <Camera />} {envoi ? 'Envoi...' : libelle}
                </Button>
            )}
        </div>
    );
}

/** Zone de signature au doigt / à la souris, envoyée en PNG à l'API. */
export function PadSignature({ valeur, surChangement }: { valeur: string | null; surChangement: (url: string | null) => void }) {
    const canvas = useRef<HTMLCanvasElement>(null);
    const dessin = useRef(false);
    const [vide, setVide] = useState(true);
    const [envoi, setEnvoi] = useState(false);

    const contexte = () => {
        const c = canvas.current!;
        const ctx = c.getContext('2d')!;
        ctx.lineWidth = 2.5;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        ctx.strokeStyle = '#111827';
        return ctx;
    };
    const point = (e: PointerEvent<HTMLCanvasElement>) => {
        const r = canvas.current!.getBoundingClientRect();
        return { x: (e.clientX - r.left) * (canvas.current!.width / r.width), y: (e.clientY - r.top) * (canvas.current!.height / r.height) };
    };

    const debut = (e: PointerEvent<HTMLCanvasElement>) => {
        if (valeur) return;
        canvas.current!.setPointerCapture(e.pointerId);
        dessin.current = true;
        const { x, y } = point(e);
        const ctx = contexte();
        ctx.beginPath();
        ctx.moveTo(x, y);
    };
    const trace = (e: PointerEvent<HTMLCanvasElement>) => {
        if (!dessin.current) return;
        const { x, y } = point(e);
        const ctx = contexte();
        ctx.lineTo(x, y);
        ctx.stroke();
        setVide(false);
    };
    const fin = () => { dessin.current = false; };

    const effacer = () => {
        const c = canvas.current!;
        c.getContext('2d')!.clearRect(0, 0, c.width, c.height);
        setVide(true);
        surChangement(null);
    };

    const valider = () => {
        setEnvoi(true);
        canvas.current!.toBlob(async blob => {
            try {
                if (!blob) throw new Error('Signature illisible.');
                const resultat = await api.envoyerFichier('livraisons', blob, 'signature.png');
                surChangement(resultat.url);
            } catch (e) {
                toast.error(e instanceof Error ? e.message : 'Envoi impossible.');
            } finally {
                setEnvoi(false);
            }
        }, 'image/png');
    };

    return (
        <div className="grid gap-2">
            <canvas
                ref={canvas}
                width={600}
                height={200}
                aria-label="Zone de signature du client"
                className="h-32 w-full touch-none rounded-md border bg-white"
                onPointerDown={debut}
                onPointerMove={trace}
                onPointerUp={fin}
                onPointerLeave={fin}
            />
            <div className="flex items-center justify-between gap-2">
                <span className="text-muted-foreground text-xs">
                    {valeur ? <span className="inline-flex items-center gap-1 text-emerald-700"><Check className="size-4" /> Signature enregistrée</span> : 'Le client signe avec le doigt.'}
                </span>
                <div className="flex gap-2">
                    <Button type="button" variant="ghost" size="sm" onClick={effacer} disabled={vide || envoi}><RotateCcw /> Effacer</Button>
                    {!valeur && (
                        <Button type="button" size="sm" onClick={valider} disabled={vide || envoi}>
                            {envoi ? <Loader2 className="animate-spin" /> : <Check />} Valider la signature
                        </Button>
                    )}
                </div>
            </div>
        </div>
    );
}

/** Lien vers une preuve : fichier protégé de l'API (ouvert avec le jeton) ou adresse web externe. */
export function LienPreuve({ url, libelle }: { url: string; libelle: string }) {
    const [ouverture, setOuverture] = useState(false);

    if (!url.startsWith(PREFIXE_INTERNE)) {
        return (
            <a href={url} target="_blank" rel="noreferrer" className="text-primary inline-flex items-center gap-1 hover:underline">
                {libelle} <ExternalLink className="size-3" />
            </a>
        );
    }

    const ouvrir = async () => {
        // Fenêtre ouverte tout de suite (sinon bloquée comme pop-up après l'attente réseau)
        const fenetre = window.open('', '_blank');
        setOuverture(true);
        try {
            const blob = await api.lireFichier(url);
            const objet = URL.createObjectURL(blob);
            if (fenetre) fenetre.location.href = objet; else window.location.href = objet;
            window.setTimeout(() => URL.revokeObjectURL(objet), 60_000);
        } catch (e) {
            fenetre?.close();
            toast.error(e instanceof Error ? e.message : 'Lecture impossible.');
        } finally {
            setOuverture(false);
        }
    };

    return (
        <button type="button" onClick={() => void ouvrir()} className="text-primary inline-flex items-center gap-1 hover:underline" disabled={ouverture}>
            {ouverture ? <Loader2 className="size-3 animate-spin" /> : null}{libelle} <ExternalLink className="size-3" />
        </button>
    );
}
