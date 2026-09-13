DO $$
DECLARE
    v_user_id uuid;
BEGIN
    SELECT id
    INTO v_user_id
    FROM public.bi_users
    WHERE lower(email) = lower('admin@empresa.com.br')
      AND is_active = TRUE;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'Conta ativa não encontrada para esse e-mail';
    END IF;

    INSERT INTO public.bi_user_modules (
        user_id,
        module_key,
        role
    )
    VALUES
        (v_user_id, 'conciliacao', 'Administrador'),
        (v_user_id, 'mkt', 'Administrador')
    ON CONFLICT (user_id, module_key)
    DO UPDATE SET
        role = EXCLUDED.role;
END $$;


-- Confira os vínculos:
SELECT
    module_key,
    role
FROM public.bi_user_modules
WHERE user_id = (
    SELECT id
    FROM public.bi_users
    WHERE lower(email) = lower('admin@empresa.com.br')
);