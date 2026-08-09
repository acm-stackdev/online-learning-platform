"use client";

import { useState } from "react";
import { UploadCloud } from "lucide-react";

import { cn } from "@/lib/utils";
import { uploadImage } from "@/lib/cloudinary";

export function ImageUpload({
  value,
  onChange,
  shape = "square",
}: {
  value: string | null;
  onChange: (url: string) => void;
  shape?: "square" | "circle";
}) {
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading(true);
    setError(null);
    try {
      const url = await uploadImage(file);
      onChange(url);
    } catch {
      setError("Upload failed. Try again.");
    } finally {
      setUploading(false);
      e.target.value = "";
    }
  }

  return (
    <div className="flex items-center gap-4">
      <div
        className={cn(
          "flex size-20 shrink-0 items-center justify-center overflow-hidden bg-muted",
          shape === "circle" ? "rounded-full" : "rounded-lg"
        )}
      >
        {value ? (
          // eslint-disable-next-line @next/next/no-img-element -- Cloudinary URL preview thumbnail, not worth next/image config for this
          <img src={value} alt="" className="size-full object-cover" />
        ) : (
          <UploadCloud className="size-6 text-muted-foreground" />
        )}
      </div>

      <div>
        <label className="inline-flex cursor-pointer items-center rounded-lg border border-input px-3 py-1.5 text-sm font-medium transition-colors hover:bg-muted">
          {uploading ? "Uploading..." : value ? "Change image" : "Upload image"}
          <input
            type="file"
            accept="image/*"
            className="hidden"
            disabled={uploading}
            onChange={handleFileChange}
          />
        </label>
        {error ? <p className="mt-1 text-xs text-destructive">{error}</p> : null}
      </div>
    </div>
  );
}
