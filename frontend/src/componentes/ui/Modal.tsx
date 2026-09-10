import { useEffect } from "react";
import type { ReactNode } from "react";
import { X } from "lucide-react";

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: ReactNode;
}

export default function Modal({ isOpen, onClose, title, description, children }: ModalProps) {
  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };

    document.addEventListener("keydown", handleKeyDown);
    document.body.style.overflow = "hidden";

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = "";
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="sp-modal-overlay" onMouseDown={onClose}>
      <div
        className="sp-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="sp-modal-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="sp-modal__header">
          <div>
            <h2 className="sp-modal__title" id="sp-modal-title">
              {title}
            </h2>
            {description && <p className="sp-modal__description">{description}</p>}
          </div>
          <button className="sp-modal__close" onClick={onClose} aria-label="Fechar">
            <X size={18} />
          </button>
        </div>
        <div className="sp-modal__body">{children}</div>
      </div>
    </div>
  );
}

