import '@testing-library/jest-dom';

// Polyfill ResizeObserver for Recharts in jsdom test environment
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
