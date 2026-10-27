import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Imagen de Docker mínima: copia solo lo necesario para ejecutar (server.js + dependencias usadas)
  output: "standalone",
  poweredByHeader: false,
  eslint: {
    // La calidad se valida con "npm run typecheck" y "npm test"
    ignoreDuringBuilds: true,
  },
};

export default nextConfig;
