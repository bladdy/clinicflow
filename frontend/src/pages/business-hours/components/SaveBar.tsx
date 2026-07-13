import { Button } from '../../../components/ui/Button';

interface Props {
  hasChanges: boolean;
  isPending: boolean;
  onSave: () => void;
}

export function SaveBar({ hasChanges, isPending, onSave }: Props) {
  return (
    <div className="flex items-center justify-end gap-3 pt-2">
      <Button variant="ghost" size="md" disabled={isPending}>
        Cancelar
      </Button>
      <Button variant="primary" size="md" isLoading={isPending} disabled={!hasChanges} onClick={onSave}>
        Guardar cambios
      </Button>
    </div>
  );
}
