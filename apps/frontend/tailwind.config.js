/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: '#1328A0',
          light: '#2B3FBF',
          dark: '#0D1C6B',
        },
        grid: {
          bg: '#0B0F1A',
          surface: '#111827',
          panel: '#1A2035',
          border: '#1E293B',
          'border-light': '#334155',
        },
        status: {
          healthy: '#10B981',
          'healthy-muted': '#065F46',
          theft: '#EF4444',
          'theft-muted': '#7F1D1D',
          overload: '#F59E0B',
          'overload-muted': '#78350F',
          linedrop: '#14B8A6',
          'linedrop-muted': '#134E4A',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
      },
    },
  },
  plugins: [],
};
