/**
 * Formats a UK number plate registration with proper spacing according to UK standards
 * @param registration The registration to format
 * @returns Formatted registration with proper spacing
 */
export function formatRegistration(registration: string | undefined): string {
  if (!registration) return '';
  
  const clean = registration.replace(/\s/g, '').toUpperCase();
  
  // Current format: AB12 ABC (2 letters, 2 numbers, 3 letters)
  if (/^[A-Z]{2}\d{2}[A-Z]{3}$/.test(clean)) {
    return clean.slice(0, 4) + ' ' + clean.slice(4);
  }
  
  // Prefix format: A123 BCD (1 letter, 1-3 numbers, 3 letters)
  const prefixMatch = clean.match(/^([A-Z])(\d{1,3})([A-Z]{3})$/);
  if (prefixMatch) {
    return `${prefixMatch[1]}${prefixMatch[2]} ${prefixMatch[3]}`;
  }
  
  // Suffix format: ABC 123D (3 letters, 1-3 numbers, 1 letter)
  const suffixMatch = clean.match(/^([A-Z]{3})(\d{1,3})([A-Z])$/);
  if (suffixMatch) {
    return `${suffixMatch[1]} ${suffixMatch[2]}${suffixMatch[3]}`;
  }
  
  // Dateless/Cherished plates - try to find a sensible split point
  // Common patterns: AB 1234, A 1, ABC 1D, etc.
  const datelessMatch = clean.match(/^([A-Z]+)(\d+[A-Z]*)$/);
  if (datelessMatch) {
    return `${datelessMatch[1]} ${datelessMatch[2]}`;
  }
  
  return registration; // Return as-is if no pattern matches
}
