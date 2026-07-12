import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../../services/api';
import { Card, CardHeader, CardContent } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Check, X, Crown, Zap, Building2, AlertTriangle } from 'lucide-react';
import { formatCurrency } from '../../utils/formatters';
import type { ApiResponse } from '../../types';

interface SubscriptionInfo {
  hasActiveSubscription: boolean;
  planName: string;
  status: string;
  startDate: string;
  endDate?: string;
  isAnnual: boolean;
  doctorsRemaining: number;
  patientsRemaining: number;
  branchesRemaining: number;
  appointmentsRemaining: number;
}

interface Plan {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  annualPrice: number;
  maxBranches: number;
  maxDoctors: number;
  maxPatients: number;
  maxAppointmentsPerMonth: number;
  maxConversationsPerMonth: number;
  hasAI: boolean;
  hasWhatsAppIntegration: boolean;
  hasAdvancedReports: boolean;
  hasPrioritySupport: boolean;
}

export function SubscriptionPage() {
  const queryClient = useQueryClient();

  const { data: subscription } = useQuery({
    queryKey: ['subscription-current'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<SubscriptionInfo>>('/subscriptions/current');
      return res.data.data;
    },
  });

  const { data: plans } = useQuery({
    queryKey: ['plans'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<{ items: Plan[] }>>('/plans?pageSize=50');
      return res.data.data?.items ?? [];
    },
  });

  useQuery({
    queryKey: ['subscription-limits'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<Record<string, unknown>>>('/subscriptions/check-limits');
      return res.data.data;
    },
  });

  const changePlanMutation = useMutation({
    mutationFn: async (planId: string) => {
      if (subscription?.hasActiveSubscription) {
        await api.post(`/subscriptions/change-plan`, { newPlanId: planId });
      } else {
        await api.post('/subscriptions', { planId, isAnnual: false });
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscription-current'] });
      queryClient.invalidateQueries({ queryKey: ['subscription-limits'] });
    },
  });

  const planIcons: Record<string, React.ReactNode> = {
    Trial: <Zap className="h-6 w-6 text-gray-500" />,
    Básico: <Check className="h-6 w-6 text-blue-500" />,
    Profesional: <Crown className="h-6 w-6 text-purple-500" />,
    Empresarial: <Building2 className="h-6 w-6 text-amber-500" />,
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Suscripción</h1>
        <p className="text-sm text-gray-500 mt-1">Gestiona tu plan y revisa el uso</p>
      </div>

      {subscription?.hasActiveSubscription && (
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">Plan Actual: {subscription.planName}</h2>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <UsageBar label="Doctores" remaining={subscription.doctorsRemaining} />
              <UsageBar label="Pacientes" remaining={subscription.patientsRemaining} />
              <UsageBar label="Sucursales" remaining={subscription.branchesRemaining} />
              <UsageBar label="Citas/mes" remaining={subscription.appointmentsRemaining} />
            </div>
            {subscription.endDate && (
              <p className="text-sm text-gray-500 mt-4">
                Renovación: {new Date(subscription.endDate).toLocaleDateString('es-MX')}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {plans?.map((plan) => (
          <Card key={plan.id} className={`relative ${subscription?.planName === plan.name ? 'ring-2 ring-blue-500' : ''}`}>
            {subscription?.planName === plan.name && (
              <div className="absolute -top-3 left-1/2 -translate-x-1/2 bg-blue-600 text-white text-xs px-3 py-1 rounded-full">
                Actual
              </div>
            )}
            <CardContent className="pt-6">
              <div className="flex items-center gap-2 mb-3">
                {planIcons[plan.name] ?? <Check className="h-6 w-6 text-gray-500" />}
                <h3 className="text-lg font-semibold">{plan.name}</h3>
              </div>
              <p className="text-sm text-gray-500 mb-4">{plan.description}</p>
              <div className="mb-4">
                <span className="text-3xl font-bold">{formatCurrency(plan.monthlyPrice)}</span>
                <span className="text-sm text-gray-500">/mes</span>
                {plan.annualPrice > 0 && (
                  <p className="text-sm text-green-600">{formatCurrency(plan.annualPrice)}/año (ahorra {Math.round((1 - plan.annualPrice / (plan.monthlyPrice * 12)) * 100)}%)</p>
                )}
              </div>
              <ul className="space-y-2 text-sm mb-6">
                <li className="flex items-center gap-2">
                  <Check className="h-4 w-4 text-green-500" />
                  {plan.maxDoctors} doctores
                </li>
                <li className="flex items-center gap-2">
                  <Check className="h-4 w-4 text-green-500" />
                  {plan.maxPatients} pacientes
                </li>
                <li className="flex items-center gap-2">
                  <Check className="h-4 w-4 text-green-500" />
                  {plan.maxBranches} sucursales
                </li>
                <li className="flex items-center gap-2">
                  {plan.hasAI ? <Check className="h-4 w-4 text-green-500" /> : <X className="h-4 w-4 text-gray-300" />}
                  IA Asistente
                </li>
                <li className="flex items-center gap-2">
                  {plan.hasWhatsAppIntegration ? <Check className="h-4 w-4 text-green-500" /> : <X className="h-4 w-4 text-gray-300" />}
                  WhatsApp
                </li>
                <li className="flex items-center gap-2">
                  {plan.hasAdvancedReports ? <Check className="h-4 w-4 text-green-500" /> : <X className="h-4 w-4 text-gray-300" />}
                  Reportes avanzados
                </li>
              </ul>
              <Button
                className="w-full"
                variant={subscription?.planName === plan.name ? 'secondary' : 'primary'}
                disabled={subscription?.planName === plan.name}
                isLoading={changePlanMutation.isPending}
                onClick={() => changePlanMutation.mutate(plan.id)}
              >
                {subscription?.planName === plan.name ? 'Plan Actual' : 'Seleccionar'}
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

function UsageBar({ label, remaining }: { label: string; remaining: number }) {
  const isLow = remaining <= 2;
  return (
    <div className={`p-3 rounded-lg ${isLow ? 'bg-yellow-50' : 'bg-gray-50'}`}>
      <p className="text-xs text-gray-500">{label}</p>
      <p className={`text-lg font-bold ${isLow ? 'text-yellow-700' : 'text-gray-900'}`}>{remaining}</p>
      <p className="text-xs text-gray-400">disponibles</p>
      {isLow && <AlertTriangle className="h-4 w-4 text-yellow-500 mt-1" />}
    </div>
  );
}
