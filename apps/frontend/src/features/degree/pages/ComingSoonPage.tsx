export function ComingSoonPage({ title = 'Degree Management' }: { title?: string }) {
  return (
    <div className="flex h-full min-h-[300px] flex-col items-center justify-center rounded-lg border border-dashed p-8 text-center animate-in fade-in-50">
      <h2 className="text-xl font-semibold text-foreground">{title}</h2>
      <p className="mt-2 text-sm text-muted-foreground">
        This feature module is under construction and will be available in upcoming phases.
      </p>
    </div>
  );
}

export default ComingSoonPage;
