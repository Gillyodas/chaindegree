import { env } from '@/app/config/env';

export function App() {
  return (
    <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>{env.appName}</h1>
      <p>Project foundation initialized successfully.</p>
    </div>
  );
}

export default App;
