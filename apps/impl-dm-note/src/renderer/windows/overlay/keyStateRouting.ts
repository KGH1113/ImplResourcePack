import type { KeyMappings } from '@src/types/key/keys';

/**
 * A dedicated viewer is authoritative for its own selected tab. The global
 * editor mode may point at the other viewer and must not suppress its input.
 */
export function isKeyMappedToViewer(
  key: string,
  selectedKeyType: string,
  keyMappings: KeyMappings,
): boolean {
  return (keyMappings[selectedKeyType] ?? []).includes(key);
}
