    const API = 'http://localhost:5000';
    const form = document.getElementById('pedido');
    const ticket = document.getElementById('ticket');
    const btnPedidos = document.getElementById('btnPedidos');
    const lista = document.getElementById('listaPedidos');

    form.addEventListener('submit', async e => {
      e.preventDefault();
      const d = new FormData(form);
      const pedido = {
        nombre: d.get('nombre'),
        pan: d.get('pan'),
        salchicha: d.get('salchicha'),
        aderezos: d.getAll('aderezo'),
        papas: d.get('papas'),
        bebida: d.get('bebida'),
        cantidad: Number(d.get('cantidad')),
        nota: d.get('nota')
      };
      try {
        const resp = await fetch(API + '/pedido', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(pedido)
        });
        if (!resp.ok) throw new Error('Error ' + resp.status);
        const data = await resp.json();
        document.getElementById('resumen').textContent = data.mensaje;
        if (!lista.hidden) cargarPedidos();
      } catch (err) {
        document.getElementById('resumen').textContent = 'No pudimos enviar el pedido. Probá de nuevo.';
      }
      ticket.hidden = false;
    });

    async function cargarPedidos() {
      lista.replaceChildren();
      try {
        const resp = await fetch(API + '/pedidos');
        if (!resp.ok) throw new Error('Error ' + resp.status);
        const pedidos = await resp.json();
        if (pedidos.length === 0) {
          agregar('Todavía no hay pedidos.', null);
        }
        pedidos.forEach(p => {
          const hora = new Date(p.hora).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
          const ad = p.aderezos.length ? 'con ' + p.aderezos.join(', ').toLowerCase() : 'sin aderezos';
          let txt = `${p.cantidad} × ${p.salchicha.toLowerCase()} en ${p.pan.toLowerCase()}, ${ad}, ${p.papas}. ${p.bebida}.`;
          if (p.nota && p.nota.trim()) txt += ` Nota: ${p.nota.trim()}.`;
          agregar(txt, `#${p.id} · ${p.nombre} · ${hora}`);
        });
      } catch (err) {
        agregar('No se pudieron cargar los pedidos. ¿Está encendido el servidor?', null);
      }
      lista.hidden = false;
      btnPedidos.textContent = 'Actualizar pedidos';
    }

    function agregar(texto, titulo) {
      const li = document.createElement('li');
      if (titulo) {
        const b = document.createElement('strong');
        b.textContent = titulo;
        li.append(b, document.createElement('br'));
      }
      li.append(texto);
      lista.append(li);
    }

    btnPedidos.addEventListener('click', cargarPedidos);