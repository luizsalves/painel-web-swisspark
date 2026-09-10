import { useState } from "react";
import type { FormEvent } from "react";
import { Mail, MessageSquareText, CheckCircle2 } from "lucide-react";
import Modal from "../ui/Modal";
import Input from "../ui/Input";
import Button from "../ui/Button";
import { sendRecoveryCode } from "../../services/authService";

type RecoveryChannel = "email" | "sms";

interface ForgotPasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function ForgotPasswordModal({ isOpen, onClose }: ForgotPasswordModalProps) {
  const [contact, setContact] = useState("");
  const [channel, setChannel] = useState<RecoveryChannel>("email");
  const [isSending, setIsSending] = useState(false);
  const [wasSent, setWasSent] = useState(false);

  function handleClose() {
    onClose();
    setTimeout(() => {
      setContact("");
      setChannel("email");
      setWasSent(false);
    }, 200);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!contact.trim()) return;

    setIsSending(true);
    await sendRecoveryCode(contact);
    setIsSending(false);
    setWasSent(true);
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Recuperar senha"
      description="Informe seu e-mail ou celular cadastrado."
    >
      {wasSent ? (
        <div className="sp-recovery-success">
          <CheckCircle2 size={36} color="var(--color-success)" />
          <p>Código enviado com sucesso.</p>
          <Button variant="secondary" fullWidth onClick={handleClose}>
            Fechar
          </Button>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="sp-recovery-form">
          <Input
            label="E-mail ou celular"
            placeholder="seu.email@swisspark.com.br"
            value={contact}
            onChange={(event) => setContact(event.target.value)}
            autoFocus
          />

          <div className="sp-recovery-channels">
            <button
              type="button"
              className={`sp-recovery-channel ${channel === "email" ? "sp-recovery-channel--active" : ""}`}
              onClick={() => setChannel("email")}
            >
              <Mail size={16} />
              E-mail
            </button>
            <button
              type="button"
              className={`sp-recovery-channel ${channel === "sms" ? "sp-recovery-channel--active" : ""}`}
              onClick={() => setChannel("sms")}
            >
              <MessageSquareText size={16} />
              SMS
            </button>
          </div>

          <Button type="submit" fullWidth isLoading={isSending} disabled={!contact.trim()}>
            Enviar código
          </Button>
        </form>
      )}
    </Modal>
  );
}
