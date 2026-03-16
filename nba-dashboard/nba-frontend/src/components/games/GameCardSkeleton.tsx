import { Card } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

export default function GameCardSkeleton() {
  return (
    <Card>
      <div className="p-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex flex-1 flex-col items-center gap-2">
            <Skeleton className="h-12 w-12 rounded-full" />
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-4 w-20" />
          </div>
          <div className="flex flex-col items-center gap-2">
            <Skeleton className="h-4 w-4" />
            <Skeleton className="h-5 w-16 rounded-full" />
          </div>
          <div className="flex flex-1 flex-col items-center gap-2">
            <Skeleton className="h-12 w-12 rounded-full" />
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-4 w-20" />
          </div>
        </div>
        <div className="mt-3 flex justify-center">
          <Skeleton className="h-3 w-28" />
        </div>
      </div>
    </Card>
  )
}
