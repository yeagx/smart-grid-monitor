import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { renderPage } from '@/test/render';
import { DecisionCenterPage } from '@/features/decisions/pages/DecisionCenterPage';

describe('DecisionCenterPage', () => {
  it('is shown as a locked feature with a disabled enable button', () => {
    renderPage(<DecisionCenterPage />, '/decisions', '/decisions');

    expect(screen.getByText('This screen is locked')).toBeInTheDocument();
    expect(screen.getByText('Closed feature')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Enable Decision Center/ })).toBeDisabled();
  });
});
