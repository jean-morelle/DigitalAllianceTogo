import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';

export function Pagination({ page, totalPages, surChangement }: { page: number; totalPages: number; surChangement: (page: number) => void }) {
    if (totalPages <= 1) return null;
    return (
        <div className="mt-4 flex items-center justify-end gap-2 text-sm">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => surChangement(page - 1)}>
                <ChevronLeft /> Précédent
            </Button>
            <span className="text-muted-foreground">Page {page} / {totalPages}</span>
            <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => surChangement(page + 1)}>
                Suivant <ChevronRight />
            </Button>
        </div>
    );
}
