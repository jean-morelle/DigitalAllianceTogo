/**
 * Client HTTP de l'API DigitalAllianceTogo.
 * - Ajoute le jeton JWT de la session.
 * - Traduit les réponses d'erreur (ProblemDetails ASP.NET Core) en ApiError lisible.
 * - Sur 401 (jeton expiré / invalide), prévient l'application pour déconnecter l'utilisateur.
 */

const CLE_SESSION = 'dat.session';

export class ApiError extends Error {
    readonly status: number;
    /** Erreurs de validation par champ (400). */
    readonly errors?: Record<string, string[]>;

    constructor(status: number, message: string, errors?: Record<string, string[]>) {
        super(message);
        this.status = status;
        this.errors = errors;
    }
}

interface ProblemDetails {
    title?: string;
    detail?: string;
    errors?: Record<string, string[]>;
}

type Parametres = Record<string, string | number | boolean | null | undefined>;

function jeton(): string | null {
    try {
        const brut = localStorage.getItem(CLE_SESSION);
        return brut ? (JSON.parse(brut) as { token?: string }).token ?? null : null;
    } catch {
        return null;
    }
}

function url(chemin: string, parametres?: Parametres): string {
    const query = new URLSearchParams();
    for (const [cle, valeur] of Object.entries(parametres ?? {})) {
        if (valeur !== undefined && valeur !== null && valeur !== '') query.set(cle, String(valeur));
    }
    const qs = query.toString();
    return `/api${chemin}${qs ? `?${qs}` : ''}`;
}

async function requete<T>(methode: string, chemin: string, corps?: unknown, parametres?: Parametres): Promise<T> {
    const entetes: Record<string, string> = { Accept: 'application/json' };
    const token = jeton();
    if (token) entetes.Authorization = `Bearer ${token}`;
    if (corps !== undefined) entetes['Content-Type'] = 'application/json';

    const reponse = await fetch(url(chemin, parametres), {
        method: methode,
        headers: entetes,
        body: corps === undefined ? undefined : JSON.stringify(corps),
    });

    if (reponse.status === 401) {
        window.dispatchEvent(new Event('dat:session-expiree'));
        throw new ApiError(401, 'Votre session a expiré, reconnectez-vous.');
    }

    if (!reponse.ok) {
        let probleme: ProblemDetails = {};
        try {
            probleme = (await reponse.json()) as ProblemDetails;
        } catch {
            /* corps vide ou non JSON */
        }
        const message = probleme.errors
            ? Object.values(probleme.errors).flat().join(' ')
            : probleme.detail ?? probleme.title ?? messageParDefaut(reponse.status);
        throw new ApiError(reponse.status, message, probleme.errors);
    }

    if (reponse.status === 204) return undefined as T;
    const texte = await reponse.text();
    return (texte ? JSON.parse(texte) : undefined) as T;
}

function messageParDefaut(status: number): string {
    switch (status) {
        case 403: return "Vous n'avez pas les droits pour cette action.";
        case 404: return 'Élément introuvable.';
        case 409: return 'Action impossible dans l\'état actuel.';
        default: return 'Une erreur est survenue, réessayez.';
    }
}

export interface FichierEnvoye {
    url: string;
    contentType: string;
    taille: number;
}

/** Envoi multipart (le navigateur fixe lui-même le Content-Type et la frontière). */
async function envoyerFichier(categorie: 'paiements' | 'livraisons', fichier: Blob, nom: string): Promise<FichierEnvoye> {
    const donnees = new FormData();
    donnees.append('fichier', fichier, nom);
    const token = jeton();
    const reponse = await fetch(`/api/fichiers/${categorie}`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
        body: donnees,
    });
    if (reponse.status === 401) {
        window.dispatchEvent(new Event('dat:session-expiree'));
        throw new ApiError(401, 'Votre session a expiré, reconnectez-vous.');
    }
    if (reponse.status === 413) throw new ApiError(413, 'Le fichier dépasse 5 Mo : réduisez la photo.');
    if (!reponse.ok) {
        const probleme = (await reponse.json().catch(() => ({}))) as ProblemDetails;
        throw new ApiError(reponse.status, probleme.errors ? Object.values(probleme.errors).flat().join(' ') : probleme.detail ?? 'Envoi impossible.');
    }
    return (await reponse.json()) as FichierEnvoye;
}

/** Fichier protégé de l'API, récupéré avec le jeton (une balise <img> ou <a> ne l'enverrait pas). */
async function lireFichier(chemin: string): Promise<Blob> {
    const token = jeton();
    const reponse = await fetch(chemin, { headers: token ? { Authorization: `Bearer ${token}` } : undefined });
    if (!reponse.ok) throw new ApiError(reponse.status, reponse.status === 404 ? 'Fichier introuvable.' : 'Lecture du fichier impossible.');
    return reponse.blob();
}

export const api = {
    envoyerFichier,
    lireFichier,
    get: <T>(chemin: string, parametres?: Parametres) => requete<T>('GET', chemin, undefined, parametres),
    post: <T = void>(chemin: string, corps?: unknown) => requete<T>('POST', chemin, corps ?? {}),
    put: <T = void>(chemin: string, corps?: unknown) => requete<T>('PUT', chemin, corps ?? {}),
    delete: <T = void>(chemin: string) => requete<T>('DELETE', chemin),
};

export { CLE_SESSION };
