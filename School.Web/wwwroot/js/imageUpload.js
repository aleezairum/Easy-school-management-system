/* ============================================================================
   Pictures picked on a page, made ready to upload: scaled down so the longest
   side is at most 1600 px and re-encoded as JPEG.

   A phone photo of a diary page is 3-10 MB; scaled like this it is a few
   hundred KB, still sharp enough to read, quick to upload on a weak line, and
   well inside WhatsApp's 5 MB limit for a picture. Every format the browser
   can open (HEIC on an iPhone that converts, PNG, WebP...) comes out as JPEG,
   which is what WhatsApp shows as a picture.
   ============================================================================ */

/** File -> { fileName, contentType, base64 } (base64 without the data: prefix). */
async function imageFileToUpload(file, maxSide = 1600, quality = 0.85) {
    const dataUrl = await new Promise((resolve, reject) => {
        const r = new FileReader();
        r.onload = () => resolve(r.result);
        r.onerror = () => reject(new Error(`Could not read ${file.name}`));
        r.readAsDataURL(file);
    });
    const img = await new Promise((resolve, reject) => {
        const i = new Image();
        i.onload = () => resolve(i);
        i.onerror = () => reject(new Error(`${file.name} is not a picture this browser can open`));
        i.src = dataUrl;
    });

    const scale = Math.min(1, maxSide / Math.max(img.naturalWidth, img.naturalHeight));
    const w = Math.max(1, Math.round(img.naturalWidth * scale));
    const h = Math.max(1, Math.round(img.naturalHeight * scale));
    const canvas = document.createElement('canvas');
    canvas.width = w; canvas.height = h;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#fff';                 // a transparent PNG gets a white page, not black
    ctx.fillRect(0, 0, w, h);
    ctx.drawImage(img, 0, 0, w, h);
    const jpeg = canvas.toDataURL('image/jpeg', quality);

    const base = (file.name || 'image').replace(/\.[^.]+$/, '');
    return { fileName: base + '.jpg', contentType: 'image/jpeg', base64: jpeg.substring(jpeg.indexOf(',') + 1), preview: jpeg };
}
