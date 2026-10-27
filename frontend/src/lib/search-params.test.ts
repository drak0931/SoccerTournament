import { describe, expect, it } from "vitest";
import { getOneOf, getPaging, hrefWith } from "./search-params";

const SORTABLE = ["fullName", "createdAt"] as const;

describe("search-params", () => {
  it("usa valores por defecto cuando no hay parámetros", () => {
    expect(getPaging({}, SORTABLE)).toEqual({ pageNumber: 1, pageSize: 10, sortBy: undefined, sortDirection: undefined });
  });

  it("acepta paginación y orden válidos", () => {
    const paging = getPaging({ pageNumber: "3", pageSize: "20", sortBy: "fullName", sortDirection: "desc" }, SORTABLE);
    expect(paging).toEqual({ pageNumber: 3, pageSize: 20, sortBy: "fullName", sortDirection: "desc" });
  });

  it("descarta valores fuera de lo permitido en lugar de enviarlos a la API", () => {
    const paging = getPaging({ pageNumber: "-1", pageSize: "1000", sortBy: "drop table", sortDirection: "sideways" }, SORTABLE);
    expect(paging).toEqual({ pageNumber: 1, pageSize: 10, sortBy: undefined, sortDirection: undefined });
  });

  it("solo devuelve valores de la lista permitida", () => {
    expect(getOneOf({ status: "finished" }, "status", ["scheduled", "finished"] as const)).toBe("finished");
    expect(getOneOf({ status: "<script>" }, "status", ["scheduled", "finished"] as const)).toBeUndefined();
  });

  it("construye enlaces conservando filtros y aplicando cambios", () => {
    const href = hrefWith("/teams", { name: "vol", pageNumber: "2", sortBy: "fullName" }, { pageNumber: 3, sortBy: undefined });
    expect(href).toBe("/teams?name=vol&pageNumber=3");
  });
});
