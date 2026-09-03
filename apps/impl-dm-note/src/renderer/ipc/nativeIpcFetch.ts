import { invoke } from '@tauri-apps/api/core';

interface NativeHttpResponse {
  status: number;
  headers: Record<string, string>;
  body: string;
}

function headersToRecord(
  headers: HeadersInit | undefined,
): Record<string, string> {
  return Object.fromEntries(new Headers(headers).entries());
}

export async function nativeIpcFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<Response> {
  const request = input instanceof Request ? input : null;
  const url = request?.url ?? String(input);
  const method = (init.method ?? request?.method ?? 'GET').toUpperCase();
  const headers = headersToRecord(init.headers ?? request?.headers);

  let body: string | undefined;
  if (typeof init.body === 'string') {
    body = init.body;
  } else if (init.body != null) {
    throw new TypeError(
      'ADOFAI-IPC native fetch only supports string request bodies',
    );
  } else if (request && method !== 'GET') {
    body = await request.clone().text();
  }

  const response = await invoke<NativeHttpResponse>('adofai_ipc_http_request', {
    request: { url, method, headers, body },
  });

  return new Response(response.body, {
    status: response.status,
    headers: response.headers,
  });
}
