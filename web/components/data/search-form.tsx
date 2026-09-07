import { Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function SearchForm({
  placeholder = "Buscar",
  defaultValue,
  hidden,
}: {
  placeholder?: string;
  defaultValue?: string;
  hidden?: Record<string, string>;
}) {
  return (
    <form className="mb-4 flex max-w-md gap-2" method="get">
      {hidden
        ? Object.entries(hidden).map(([name, value]) => (
            <input key={name} type="hidden" name={name} value={value} />
          ))
        : null}
      <div className="relative flex-1">
        <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          name="termo"
          defaultValue={defaultValue}
          placeholder={placeholder}
          className="h-10 rounded-[4px] pl-8"
          aria-label="Buscar"
        />
      </div>
      <Button type="submit" className="h-10 rounded-[4px] px-4">
        Buscar
      </Button>
    </form>
  );
}
