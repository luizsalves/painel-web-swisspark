import { useEffect, useRef, useState } from "react";
import { ChevronDown, LogOut, User as UserIcon } from "lucide-react";
import { useAuth } from "../../contexts/AuthContext";
import { useNavigate } from "react-router-dom";

export default function UserMenu() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (!user) return null;

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <div className="sp-user-menu" ref={menuRef}>
      <button
        className="sp-user-menu__trigger"
        onClick={() => setIsOpen((prev) => !prev)}
        aria-haspopup="menu"
        aria-expanded={isOpen}
      >
        <span className="sp-user-menu__avatar">{user.avatarInitials}</span>
        <span className="sp-user-menu__info">
          <span className="sp-user-menu__name">{user.name}</span>
          <span className="sp-user-menu__role">{user.role}</span>
        </span>
        <ChevronDown size={16} className="sp-user-menu__chevron" />
      </button>

      {isOpen && (
        <div className="sp-user-menu__dropdown" role="menu">
          <button className="sp-user-menu__item" role="menuitem">
            <UserIcon size={16} />
            Meu Perfil
          </button>
          <button className="sp-user-menu__item sp-user-menu__item--danger" role="menuitem" onClick={handleLogout}>
            <LogOut size={16} />
            Sair
          </button>
        </div>
      )}
    </div>
  );
}

