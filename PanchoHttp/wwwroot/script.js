const form = document.getElementById('pedido');
const ticket = document.getElementById('ticket');
const resumen = document.getElementById('resumen');

form.addEventListener('submit', async event => {
  event.preventDefault();
  const data = new FormData(form);
  const pedido = {
    nombre: data.get('nombre'),
    pan: data.get('pan'),
    salchicha: data.get('salchicha'),
    aderezos: data.getAll('aderezo'),
    papas: data.get('papas'),
    bebida: data.get('bebida'),
    cantidad: Number(data.get('cantidad')),
    nota: data.get('nota')
  };

  try {
    const response = await fetch('/pedido', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(pedido)
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    resumen.textContent = 'Solicitud recibida y registrada.';
  } catch {
    resumen.textContent = 'No pudimos enviar la solicitud. Probá de nuevo.';
  }
  ticket.hidden = false;
});
