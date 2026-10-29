import { Providers } from './providers';
import { AppRouter } from './router';

export function App() {
  return (
    <Providers>
      <AppRouter />
    </Providers>
  );
}
