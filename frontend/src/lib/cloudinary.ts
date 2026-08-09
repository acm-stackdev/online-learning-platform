// Client-direct-to-Cloudinary upload, using an unsigned upload preset — the
// backend never touches these bytes, only receives the resulting URL. Same
// convention as course thumbnails/avatars everywhere else in the app.
export async function uploadImage(file: File): Promise<string> {
  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
  const uploadPreset = process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET;

  const form = new FormData();
  form.append("file", file);
  form.append("upload_preset", uploadPreset!);

  const res = await fetch(`https://api.cloudinary.com/v1_1/${cloudName}/image/upload`, {
    method: "POST",
    body: form,
  });

  if (!res.ok) {
    throw new Error("Upload failed. Please try again.");
  }

  const data: { secure_url: string } = await res.json();
  return data.secure_url;
}
